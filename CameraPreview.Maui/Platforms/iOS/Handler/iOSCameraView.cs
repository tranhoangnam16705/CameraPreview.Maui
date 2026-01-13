using AVFoundation;
using CameraPreview.Maui.Controls;
using CameraPreview.Maui.Models;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using System.Diagnostics;
using UIKit;

namespace CameraPreview.Maui.Platforms.iOS.Handler
{
    /// <summary>
    /// Native iOS Camera implementation using modern AVFoundation APIs (iOS 17+)
    /// </summary>
    public class iOSCameraView : UIView
    {
        private CameraView _cameraView;
        private AVCaptureSession _captureSession;
        private AVCaptureDevice _captureDevice;
        private AVCaptureDeviceInput _deviceInput;
        private AVCaptureVideoDataOutput _videoOutput;
        private AVCapturePhotoOutput _photoOutput;
        private AVCaptureVideoPreviewLayer _previewLayer;
        private CameraFrameDelegate _frameDelegate;

        private CameraFrameEventArgs _lastCameraFrame;
        private readonly object _frameLock = new();

        private bool _isInitialized = false;
        private bool _isRunning = false;
        private bool _useFrontCamera = false;

        // Keep strong reference to photo delegate to prevent GC
        private PhotoCaptureDelegate _currentPhotoDelegate;

        public event EventHandler<CameraFrameEventArgs> FrameReady;
        public event EventHandler CameraStarted;
        public event EventHandler CameraStopped;
        public event EventHandler<string> CameraError;
        public event EventHandler<string> TakePhotoSaved;

        public iOSCameraView(CameraView cameraView)
        {
            _cameraView = cameraView;
            BackgroundColor = UIColor.Black;

            Debug.WriteLine("iOSCameraView created");
            InitializeDevices();
        }

        #region Device Discovery

        private void InitializeDevices()
        {
            try
            {
                var discoverySession = AVCaptureDeviceDiscoverySession.Create(
                    new[] { AVCaptureDeviceType.BuiltInWideAngleCamera },
                    AVMediaTypes.Video,
                    AVCaptureDevicePosition.Unspecified);

                var devices = discoverySession?.Devices;
                if (devices == null || devices.Length == 0)
                {
                    Debug.WriteLine("No camera devices found");
                    return;
                }

                foreach (var device in devices)
                {
                    var position = device.Position switch
                    {
                        AVCaptureDevicePosition.Back => CameraPreviewPosition.Back,
                        AVCaptureDevicePosition.Front => CameraPreviewPosition.Front,
                        _ => CameraPreviewPosition.Unknow
                    };

                    _cameraView.Cameras.Add(new CameraPreviewInfo
                    {
                        Name = device.LocalizedName,
                        DeviceId = device.UniqueID,
                        Position = position,
                        HasFlashUnit = device.FlashAvailable,
                        MinZoomFactor = (float)device.MinAvailableVideoZoomFactor,
                        MaxZoomFactor = (float)device.MaxAvailableVideoZoomFactor,
                        HorizontalViewAngle = device.ActiveFormat.VideoFieldOfView * MathF.PI / 180f,
                        AvailableResolutions = new() { new(1920, 1080), new(1280, 720), new(640, 480) }
                    });
                }

                _isInitialized = true;
                _cameraView.RefreshDevices();
                Debug.WriteLine($"Initialized {_cameraView.Cameras.Count} iOS cameras");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeDevices error: {ex.Message}");
                RaiseError($"Device initialization failed: {ex.Message}");
            }
        }

        #endregion Device Discovery

        #region Camera Control

        public bool UseFrontCamera
        {
            get => _useFrontCamera;
            set
            {
                if (_useFrontCamera != value)
                {
                    _useFrontCamera = value;
                    if (_isRunning)
                    {
                        StopCamera();
                        _ = Task.Delay(300).ContinueWith(_ => StartAsync());
                    }
                }
            }
        }

        public bool IsRunning => _isRunning;

        public async Task StartAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    RaiseError("Camera not initialized");
                    return;
                }

                if (!await CameraView.RequestPermissions())
                {
                    RaiseError("Camera permission denied");
                    return;
                }

                if (_cameraView.Camera == null)
                {
                    RaiseError("No camera selected");
                    return;
                }

                StopCamera();

                Debug.WriteLine("Starting iOS camera...");
                await Task.Run(() => SetupCaptureSession());

                if (_captureSession != null)
                {
                    _captureSession.StartRunning();
                    _isRunning = true;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CameraStarted?.Invoke(this, EventArgs.Empty);
                    });

                    Debug.WriteLine($"Camera started: {(_useFrontCamera ? "Front" : "Back")}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Start camera error: {ex.Message}");
                RaiseError($"Start failed: {ex.Message}");
            }
        }

        public void StopCamera()
        {
            try
            {
                if (_captureSession?.Running == true)
                {
                    _captureSession.StopRunning();
                }

                CleanupCaptureSession();

                _isRunning = false;

                Debug.WriteLine("Camera stopped");
                CameraStopped?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Stop camera error: {ex.Message}");
            }
        }

        public void SetCamera(CameraPreviewInfo camera)
        {
            _cameraView.Camera = camera;
        }

        #endregion Camera Control

        #region Capture Session Setup

        private void SetupCaptureSession()
        {
            try
            {
                _useFrontCamera = _cameraView.Camera!.Position == CameraPreviewPosition.Front;

                // Create capture session
                _captureSession = new AVCaptureSession();
                _captureSession.BeginConfiguration();

                try
                {
                    _captureSession.SessionPreset = AVCaptureSession.Preset1280x720;

                    // Setup camera input
                    if (!SetupCameraInput())
                    {
                        _captureSession.CommitConfiguration();
                        return;
                    }

                    // Setup outputs
                    SetupPhotoOutput();
                    SetupVideoDataOutput();

                    _captureSession.CommitConfiguration();

                    // Setup preview layer on main thread
                    MainThread.BeginInvokeOnMainThread(() => SetupPreviewLayer());
                }
                catch
                {
                    _captureSession.CommitConfiguration();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetupCaptureSession error: {ex.Message}");
                RaiseError($"Setup failed: {ex.Message}");
            }
        }

        private bool SetupCameraInput()
        {
            try
            {
                var position = _useFrontCamera
                    ? AVCaptureDevicePosition.Front
                    : AVCaptureDevicePosition.Back;

                _captureDevice = GetCameraDevice(position);
                if (_captureDevice == null)
                {
                    RaiseError("No camera device found");
                    return false;
                }

                // Lock device for configuration
                NSError error = null;
                if (!_captureDevice.LockForConfiguration(out error))
                {
                    RaiseError($"Failed to lock device: {error?.LocalizedDescription}");
                    return false;
                }

                // Set autofocus if supported
                if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                {
                    _captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                }

                // Set auto exposure if supported
                if (_captureDevice.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
                {
                    _captureDevice.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                }

                _captureDevice.UnlockForConfiguration();

                // Create device input
                _deviceInput = AVCaptureDeviceInput.FromDevice(_captureDevice, out error);
                if (error != null || _deviceInput == null)
                {
                    RaiseError($"Failed to create device input: {error?.LocalizedDescription}");
                    return false;
                }

                if (_captureSession!.CanAddInput(_deviceInput))
                {
                    _captureSession.AddInput(_deviceInput);
                    return true;
                }
                else
                {
                    RaiseError("Cannot add device input to session");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetupCameraInput error: {ex.Message}");
                return false;
            }
        }

        private void SetupPhotoOutput()
        {
            try
            {
                _photoOutput = new AVCapturePhotoOutput();

                if (_captureSession!.CanAddOutput(_photoOutput))
                {
                    _captureSession.AddOutput(_photoOutput);

                    // Configure photo connection for orientation
                    ConfigurePhotoConnection();

                    Debug.WriteLine("Photo output configured");
                }
                else
                {
                    Debug.WriteLine("Cannot add photo output to session");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetupPhotoOutput error: {ex.Message}");
            }
        }

        private void ConfigurePhotoConnection()
        {
            try
            {
                var connection = _photoOutput?.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant()!);
                if (connection == null) return;

                // Set video orientation/rotation based on iOS version
                if (OperatingSystem.IsIOSVersionAtLeast(17))
                {
                    // iOS 17+: Use VideoRotationAngle (90 degrees = portrait)
                    const float portraitAngle = 90f;
                    if (connection.IsVideoRotationAngleSupported(portraitAngle))
                    {
                        connection.VideoRotationAngle = portraitAngle;
                    }
                }
                else
                {
                    // iOS 15-16: Use deprecated VideoOrientation
#pragma warning disable CA1422
                    if (connection.SupportsVideoOrientation)
                    {
                        connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;
                    }
#pragma warning restore CA1422
                }

                // Enable video mirroring for front camera
                if (connection.SupportsVideoMirroring && _useFrontCamera)
                {
                    connection.VideoMirrored = true;
                }

                Debug.WriteLine($"Photo connection configured - Mirrored: {connection.VideoMirrored}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConfigurePhotoConnection error: {ex.Message}");
            }
        }

        private void SetupVideoDataOutput()
        {
            try
            {
                _videoOutput = new AVCaptureVideoDataOutput
                {
                    AlwaysDiscardsLateVideoFrames = true
                };

                // Use BGRA format for efficient processing
                _videoOutput.WeakVideoSettings = new CVPixelBufferAttributes
                {
                    PixelFormatType = CVPixelFormatType.CV32BGRA
                }.Dictionary;

                // Create frame delegate
                _frameDelegate = new CameraFrameDelegate(this);
                var queue = new CoreFoundation.DispatchQueue("com.camerapreview.videoqueue");
                _videoOutput.SetSampleBufferDelegate(_frameDelegate, queue);

                if (_captureSession!.CanAddOutput(_videoOutput))
                {
                    _captureSession.AddOutput(_videoOutput);

                    // Configure video connection for proper orientation
                    ConfigureVideoConnection();

                    Debug.WriteLine("Video output configured");
                }
                else
                {
                    Debug.WriteLine("Cannot add video output to session");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetupVideoDataOutput error: {ex.Message}");
            }
        }

        private void ConfigureVideoConnection()
        {
            try
            {
                var connection = _videoOutput?.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant()!);
                if (connection == null) return;

                bool isPortraitOrientation = false;

                // Set video orientation/rotation based on iOS version
                if (OperatingSystem.IsIOSVersionAtLeast(17))
                {
                    // iOS 17+: Use VideoRotationAngle (90 degrees = portrait)
                    const float portraitAngle = 90f;
                    if (connection.IsVideoRotationAngleSupported(portraitAngle))
                    {
                        connection.VideoRotationAngle = portraitAngle;
                        isPortraitOrientation = true;
                    }
                }
                else
                {
                    // iOS 15-16: Use deprecated VideoOrientation
#pragma warning disable CA1422
                    if (connection.SupportsVideoOrientation)
                    {
                        connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;
                        isPortraitOrientation = true;
                    }
#pragma warning restore CA1422
                }

                // Notify frame delegate about orientation for correct width/height calculation
                _frameDelegate?.SetPortraitOrientation(isPortraitOrientation);

                // Enable video mirroring for front camera
                bool isMirrored = false;
                if (connection.SupportsVideoMirroring && _useFrontCamera)
                {
                    connection.VideoMirrored = true;
                    isMirrored = true;
                }

                // Notify frame delegate about mirroring for correct image transformation
                _frameDelegate?.SetMirrored(isMirrored);

                Debug.WriteLine($"Video connection configured - Portrait: {isPortraitOrientation}, Mirrored: {isMirrored}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConfigureVideoConnection error: {ex.Message}");
            }
        }

        private AVCaptureDevice GetCameraDevice(AVCaptureDevicePosition position)
        {
            var discoverySession = AVCaptureDeviceDiscoverySession.Create(
                new[] { AVCaptureDeviceType.BuiltInWideAngleCamera },
                AVMediaTypes.Video,
                position);

            return discoverySession?.Devices?.FirstOrDefault();
        }

        private void SetupPreviewLayer()
        {
            if (_previewLayer != null)
            {
                _previewLayer.RemoveFromSuperLayer();
                _previewLayer.Dispose();
                _previewLayer = null;
            }

            if (_captureSession == null) return;

            _previewLayer = new AVCaptureVideoPreviewLayer(_captureSession)
            {
                Frame = Bounds,
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };

            Layer.InsertSublayer(_previewLayer, 0);
        }

        private void CleanupCaptureSession()
        {
            if (_deviceInput != null)
            {
                _captureSession?.RemoveInput(_deviceInput);
                _deviceInput?.Dispose();
                _deviceInput = null;
            }

            if (_videoOutput != null)
            {
                _captureSession?.RemoveOutput(_videoOutput);
                _videoOutput?.Dispose();
                _videoOutput = null;
            }

            if (_photoOutput != null)
            {
                _captureSession?.RemoveOutput(_photoOutput);
                _photoOutput?.Dispose();
                _photoOutput = null;
            }

            _frameDelegate?.Dispose();
            _frameDelegate = null;

            _captureDevice?.Dispose();
            _captureDevice = null;
        }

        #endregion Capture Session Setup

        #region Layout

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (_previewLayer != null)
            {
                _previewLayer.Frame = Bounds;
            }
        }

        #endregion Layout

        #region Snapshot

        public ImageSource GetSnapShot(ImageFormat imageFormat, bool auto = false)
        {
            if (!IsRunning || _lastCameraFrame == null)
                return null;

            lock (_frameLock)
            {
                try
                {
                    return ImageSource.FromStream(() => new MemoryStream(_lastCameraFrame.ImageData));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GetSnapShot error: {ex.Message}");
                    return null;
                }
            }
        }

        public bool SaveSnapShot(ImageFormat imageFormat, string snapFilePath)
        {
            if (!IsRunning || _lastCameraFrame == null)
                return false;

            try
            {
                lock (_frameLock)
                {
                    if (File.Exists(snapFilePath))
                        File.Delete(snapFilePath);

                    using var nsData = NSData.FromArray(_lastCameraFrame.ImageData);
                    using var image = UIImage.LoadFromData(nsData);

                    if (image == null)
                        return false;

                    var data = imageFormat == ImageFormat.Png
                        ? image.AsPNG()
                        : image.AsJPEG(0.95f);

                    if (data == null)
                        return false;

                    using var fileUrl = NSUrl.FromFilename(snapFilePath);
                    return data.Save(fileUrl, true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveSnapShot error: {ex.Message}");
                return false;
            }
        }

        #endregion Snapshot

        #region Photo Capture

        public Task<Stream> TakePhotoAsync(ImageFormat format)
        {
            if (_photoOutput == null)
            {
                Debug.WriteLine("TakePhotoAsync: Photo output is null");
                return Task.FromException<Stream>(new InvalidOperationException("Photo output not initialized"));
            }

            if (!_isRunning || _captureSession?.Running != true)
            {
                Debug.WriteLine("TakePhotoAsync: Camera session not running");
                return Task.FromException<Stream>(new InvalidOperationException("Camera session not running"));
            }

            var tcs = new TaskCompletionSource<Stream>();

            try
            {
                Debug.WriteLine("TakePhotoAsync: Starting photo capture...");

                // Must be called on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        var settings = AVCapturePhotoSettings.Create();
                        settings.FlashMode = AVCaptureFlashMode.Off;

                        // Keep strong reference to prevent GC before callback
                        _currentPhotoDelegate = new PhotoCaptureDelegate(this, tcs, format);
                        _photoOutput.CapturePhoto(settings, _currentPhotoDelegate);
                        Debug.WriteLine("TakePhotoAsync: CapturePhoto called");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"TakePhotoAsync MainThread error: {ex.Message}");
                        tcs.TrySetException(ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TakePhotoAsync error: {ex.Message}");
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        #endregion Photo Capture

        #region Event Handlers

        private void RaiseError(string message)
        {
            Debug.WriteLine($"Camera error: {message}");
            CameraError?.Invoke(this, message);
        }

        internal void OnFrameAnalyzed(CameraFrameEventArgs args)
        {
            lock (_frameLock)
            {
                _lastCameraFrame = args;
            }
                FrameReady?.Invoke(this, args);
            }

        internal void OnPhotoSaved(string filePath)
        {
            TakePhotoSaved?.Invoke(this, filePath);
        }

        #endregion Event Handlers

        #region Disposal

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopCamera();

                _previewLayer?.RemoveFromSuperLayer();
                _previewLayer?.Dispose();
                _previewLayer = null;

                _captureSession?.Dispose();
                _captureSession = null;
            }
            base.Dispose(disposing);
        }

        #endregion Disposal

        #region Frame Delegate

        private class CameraFrameDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
        {
            private readonly iOSCameraView _cameraView;
            private bool _isProcessing = false;
            private int _frameCounter = 0;
            private const int FRAME_SKIP = 2; // Process every 3rd frame
            private bool _isPortraitOrientation = true; // Track if 90/270 rotation is applied
            private bool _isMirrored = false; // Track if front camera mirroring is applied

            // Cache CIContext for better performance - creating new context each frame is expensive
            private CIContext _ciContext;
            private CGColorSpace _colorSpace;

            public CameraFrameDelegate(iOSCameraView cameraView)
            {
                _cameraView = cameraView;
                _ciContext = CIContext.Create();
                _colorSpace = CGColorSpace.CreateDeviceRGB();
            }

            public void SetPortraitOrientation(bool isPortrait)
            {
                _isPortraitOrientation = isPortrait;
            }

            public void SetMirrored(bool isMirrored)
            {
                _isMirrored = isMirrored;
            }

            public override void DidOutputSampleBuffer(
                AVCaptureOutput captureOutput,
                CMSampleBuffer sampleBuffer,
                AVCaptureConnection connection)
            {
                _frameCounter++;

                // Log every 50 frames to monitor camera is alive
                if (_frameCounter % 50 == 0)
                {
                    Debug.WriteLine($"[Camera] Frame #{_frameCounter} received, isProcessing: {_isProcessing}");
                }

                if (sampleBuffer == null)
                {
                    Debug.WriteLine("[DidOutputSampleBuffer] sampleBuffer is NULL!");
                    return;
                }

                try
                {
                    // Skip frames to reduce CPU load (process every 3rd frame)
                    if (_frameCounter % (FRAME_SKIP + 1) != 0)
                    {
                        return;
                    }

                    if (_isProcessing)
                    {
                        return;
                    }

                    _isProcessing = true;

                    ProcessFrame(sampleBuffer);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Frame processing error: {ex.Message}\n{ex.StackTrace}");
                }
                finally
                {
                    // Always dispose sampleBuffer - only once in finally block
                    sampleBuffer?.Dispose();
                    _isProcessing = false;
                }
            }

            private void ProcessFrame(CMSampleBuffer sampleBuffer)
            {
                try
                {
                    var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
                    if (pixelBuffer == null)
                    {
                        return;
                    }

                    // Get actual buffer dimensions (after VideoRotationAngle is applied)
                    var bufferWidth = (int)pixelBuffer.Width;
                    var bufferHeight = (int)pixelBuffer.Height;

                    // Debug: log dimensions to verify
                    if (_frameCounter % 100 == 0)
                    {
                        Debug.WriteLine($"[Frame] PixelBuffer: {bufferWidth}x{bufferHeight}, Portrait: {_isPortraitOrientation}, Mirror: {_isMirrored}");
                    }

                    // Convert to JPEG
                    var jpegData = ConvertPixelBufferToJPEG(pixelBuffer, _isPortraitOrientation, _isMirrored);
                    if (jpegData == null)
                    {
                        Debug.WriteLine("[ProcessFrame] jpegData is NULL");
                        return;
                    }

                    // Copy data immediately to avoid reference issues
                    var imageBytes = jpegData.ToArray();
                    jpegData.Dispose();

                    // Use actual pixel buffer dimensions
                    // VideoRotationAngle should have already rotated the buffer if supported
                    var args = new CameraFrameEventArgs
                    {
                        ImageData = imageBytes,
                        Width = bufferWidth,
                        Height = bufferHeight,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        RotationDegrees = 0
                    };

                    // Raise event directly - let the subscriber handle threading
                    _cameraView.OnFrameAnalyzed(args);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ProcessFrame error: {ex.Message}\n{ex.StackTrace}");
                }
            }

            private NSData ConvertPixelBufferToJPEG(CVPixelBuffer pixelBuffer, bool rotateToPortrait, bool mirror)
            {
                CIImage ciImage = null;
                try
                {
                    // Use CIImage and CIContext for efficient conversion
                    ciImage = CIImage.FromImageBuffer(pixelBuffer);
                    if (ciImage == null)
                        return null;

                    // No transformation needed - VideoRotationAngle handles rotation
                    // and VideoMirrored handles front camera mirroring on the connection

                    // Use cached context and colorSpace for better performance
                    var jpegData = _ciContext.GetJpegRepresentation(
                        ciImage,
                        _colorSpace,
                        new NSDictionary()
                    );

                    return jpegData;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ConvertPixelBufferToJPEG error: {ex.Message}");
                    return null;
                }
                finally
                {
                    ciImage?.Dispose();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _ciContext?.Dispose();
                    _ciContext = null;
                    _colorSpace?.Dispose();
                    _colorSpace = null;
                }
                base.Dispose(disposing);
            }
        }

        #endregion Frame Delegate

        #region Photo Capture Delegate

        private class PhotoCaptureDelegate : AVCapturePhotoCaptureDelegate
        {
            private readonly TaskCompletionSource<Stream> _tcs;
            private readonly ImageFormat _format;
            private readonly iOSCameraView _cameraView;

            public PhotoCaptureDelegate(
                iOSCameraView cameraView,
                TaskCompletionSource<Stream> tcs,
                ImageFormat format)
            {
                _tcs = tcs;
                _format = format;
                _cameraView = cameraView;
            }

            public override void DidFinishProcessingPhoto(
                AVCapturePhotoOutput output,
                AVCapturePhoto photo,
                NSError error)
            {
                Debug.WriteLine("PhotoCaptureDelegate: DidFinishProcessingPhoto called");

                try
                {
                    if (error != null)
                    {
                        Debug.WriteLine($"PhotoCaptureDelegate: Error - {error.LocalizedDescription}");
                        _tcs.TrySetException(new NSErrorException(error));
                        return;
                    }

                    var photoData = photo.FileDataRepresentation;
                    if (photoData == null)
                    {
                        Debug.WriteLine("PhotoCaptureDelegate: photoData is null");
                        _tcs.TrySetException(new Exception("Failed to get photo data"));
                        return;
                    }

                    Debug.WriteLine($"PhotoCaptureDelegate: Got photo data, size = {photoData.Length}");

                    // Convert to UIImage for format conversion if needed
                    using var originalImage = UIImage.LoadFromData(photoData);
                    if (originalImage == null)
                    {
                        _tcs.TrySetException(new Exception("Failed to decode image"));
                        return;
                    }

                    // Note: Orientation and mirroring are already handled by AVCaptureConnection
                    // No manual transformation needed

                    // Convert to requested format
                    var finalData = _format == ImageFormat.Png
                        ? originalImage.AsPNG()
                        : originalImage.AsJPEG(0.95f);

                    if (finalData == null)
                    {
                        _tcs.TrySetException(new Exception("Failed to convert image format"));
                        return;
                    }

                    // Save to temp file for event notification
                    var tempPath = CreateTempFilePath(_format);
                    var imageBytes = finalData.ToArray();
                    File.WriteAllBytes(tempPath, imageBytes);

                    // Return stream from copied bytes (not from NSData which will be disposed)
                    var stream = new MemoryStream(imageBytes);
                    _tcs.TrySetResult(stream);

                    // Notify photo saved
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _cameraView.OnPhotoSaved(tempPath);
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Photo capture error: {ex.Message}");
                    _tcs.TrySetException(ex);
                }
            }

            private string CreateTempFilePath(ImageFormat format)
            {
                var extension = format == ImageFormat.Png ? ".png" : ".jpg";
                var tempDir = NSSearchPath.GetDirectories(
                    NSSearchPathDirectory.CachesDirectory,
                    NSSearchPathDomain.User)[0];

                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                var fileName = $"camera_{Guid.NewGuid():N}{extension}";
                return Path.Combine(tempDir, fileName);
            }
        }

        #endregion Photo Capture Delegate
    }
}