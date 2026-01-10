using AVFoundation;
using CameraPreview.Maui.Controls;
using CameraPreview.Maui.Models;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using System.Diagnostics;
using UIKit;

namespace CameraPreview.Maui.Platforms.iOS.Handler
{
    /// <summary>
    /// Native iOS Camera implementation using AVFoundation
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
        private CameraFrameEventArgs lastCameraFrame;
        private readonly object lockCapture = new();

        private bool _initiated = false;
        private bool _isRunning = false;
        private bool _useFrontCamera = false;

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
            InitDevices();
        }

        private void InitDevices()
        {
            try
            {
                var discoverySession = AVCaptureDeviceDiscoverySession.Create(
                    new[] { AVCaptureDeviceType.BuiltInWideAngleCamera },
                    AVMediaTypes.Video,
                    AVCaptureDevicePosition.Unspecified);

                var devices = discoverySession?.Devices;
                if (devices == null) return;

                foreach (var device in devices)
                {
                    CameraPreviewPosition position = device.Position switch
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
                        AvailableResolutions = new() { new(1920, 1080), new(1280, 720), new(640, 480), new(352, 288) }
                    });
                }

                _initiated = true;
                _cameraView.RefreshDevices();
                Debug.WriteLine($"Initialized {_cameraView.Cameras.Count} iOS cameras");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitDevices error: {ex.Message}");
            }
        }

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
                        Task.Delay(300).ContinueWith(_ => StartAsync());
                    }
                }
            }
        }

        public bool IsRunning => _isRunning;

        public async Task StartAsync()
        {
            try
            {
                if (!_initiated) return;

                if (await CameraView.RequestPermissions())
                {
                    StopCamera();

                    if (_cameraView.Camera != null)
                    {
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
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Start camera error: {ex.Message}");
                RaiseError($"Start failed: {ex.Message}");
            }
        }

        private void SetupCaptureSession()
        {
            try
            {
                _useFrontCamera = _cameraView.Camera.Position == CameraPreviewPosition.Front;

                // Create capture session
                _captureSession = new AVCaptureSession
                {
                    SessionPreset = AVCaptureSession.Preset1280x720
                };

                // Get camera device
                var position = _useFrontCamera
                    ? AVCaptureDevicePosition.Front
                    : AVCaptureDevicePosition.Back;

                _captureDevice = GetCameraDevice(position);
                if (_captureDevice == null)
                {
                    RaiseError("No camera device found");
                    return;
                }

                // Create device input
                NSError error;
                _deviceInput = new AVCaptureDeviceInput(_captureDevice, out error);
                if (error != null)
                {
                    RaiseError($"Failed to create device input: {error.LocalizedDescription}");
                    return;
                }

                if (_captureSession.CanAddInput(_deviceInput))
                {
                    _captureSession.AddInput(_deviceInput);
                }
                else
                {
                    RaiseError("Cannot add device input to session");
                    return;
                }
                _photoOutput = new AVCapturePhotoOutput();
                if (_captureSession.CanAddOutput(_photoOutput))
                {
                    _captureSession.AddOutput(_photoOutput);
                }
                else
                {
                    RaiseError("Cannot add photo output to session");
                    return;
                }

                // Create video output
                _videoOutput = new AVCaptureVideoDataOutput
                {
                    AlwaysDiscardsLateVideoFrames = true
                };

                // Set pixel format
                _videoOutput.WeakVideoSettings = new CVPixelBufferAttributes
                {
                    PixelFormatType = CVPixelFormatType.CV32BGRA
                }.Dictionary;

                // Create frame delegate
                _frameDelegate = new CameraFrameDelegate(this);
                var queue = new CoreFoundation.DispatchQueue("CameraQueue");
                _videoOutput.SetSampleBufferDelegate(_frameDelegate, queue);
                if (_captureSession.CanAddOutput(_videoOutput))
                {
                    _captureSession.AddOutput(_videoOutput);
                }
                else
                {
                    RaiseError("Cannot add video output to session");
                    return;
                }

                // Setup preview layer on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SetupPreviewLayer();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetupCaptureSession error: {ex.Message}");
                RaiseError($"Setup failed: {ex.Message}");
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
            }

            _previewLayer = new AVCaptureVideoPreviewLayer(_captureSession)
            {
                Frame = Bounds,
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };

            Layer.InsertSublayer(_previewLayer, 0);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (_previewLayer != null)
            {
                _previewLayer.Frame = Bounds;
            }
        }

        public void SetCamera(CameraPreviewInfo camera)
        {
            _cameraView.Camera = camera;
        }

        public void StopCamera()
        {
            try
            {
                if (_captureSession?.Running == true)
                {
                    _captureSession.StopRunning();
                }

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

                _frameDelegate?.Dispose();
                _frameDelegate = null;

                _captureDevice?.Dispose();
                _captureDevice = null;

                _isRunning = false;

                Debug.WriteLine("Camera stopped");
                CameraStopped?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Stop camera error: {ex.Message}");
            }
        }

        private void RaiseError(string message)
        {
            Debug.WriteLine($"Camera error: {message}");
            CameraError?.Invoke(this, message);
        }

        internal void OnFrameAnalyzed(CameraFrameEventArgs args)
        {
            lastCameraFrame = args;
            FrameReady?.Invoke(this, args);
        }

        internal void OnPhotoSaved(string filePath)
        {
            TakePhotoSaved?.Invoke(this, filePath);
        }

        public ImageSource GetSnapShot(ImageFormat imageFormat, bool auto = false)
        {
            ImageSource result = null;

            if (IsRunning && lastCameraFrame != null)
            {
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    lock (lockCapture)
                    {
                        result = ImageSource.FromStream(() => new MemoryStream(lastCameraFrame.ImageData));
                    }
                }).Wait();
            }

            return result;
        }

        public bool SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
        {
            bool result = true;

            if (IsRunning && lastCameraFrame != null)
            {
                if (File.Exists(SnapFilePath)) File.Delete(SnapFilePath);
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        lock (lockCapture)
                        {
                            using var nsData = NSData.FromArray(lastCameraFrame.ImageData);
                            var image2 = UIImage.LoadFromData(nsData);
                            switch (imageFormat)
                            {
                                case ImageFormat.Png:
                                    image2.AsPNG().Save(NSUrl.FromFilename(SnapFilePath), true);
                                    break;

                                case ImageFormat.Jpeg:
                                    image2.AsJPEG().Save(NSUrl.FromFilename(SnapFilePath), true);
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        result = false;
                    }
                }).Wait();
            }
            else
                result = false;
            return result;
        }

        public Task<Stream> TakePhotoAsync(ImageFormat format)
        {
            var tcs = new TaskCompletionSource<Stream>();

            var settings = AVCapturePhotoSettings.Create();
            //settings.FlashMode = cameraView.FlashMode switch
            //{
            //    FlashMode.Auto => AVCaptureFlashMode.Auto,
            //    FlashMode.Enabled => AVCaptureFlashMode.On,
            //    _ => AVCaptureFlashMode.Off
            //};
            settings.FlashMode = AVCaptureFlashMode.Off;

            _photoOutput.CapturePhoto(
                settings,
                new PhotoCaptureDelegate(this, tcs, format));

            return tcs.Task;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopCamera();
                _previewLayer?.RemoveFromSuperLayer();
                _previewLayer?.Dispose();
                _captureSession?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Frame Delegate

        private class CameraFrameDelegate : AVCaptureVideoDataOutputSampleBufferDelegate
        {
            private readonly iOSCameraView _cameraView;
            private bool _isProcessing = false;
            private int _skipFrames = 0;
            private const int SKIP_FRAME_COUNT = 2; // Process every 3rd frame

            public CameraFrameDelegate(iOSCameraView cameraView)
            {
                _cameraView = cameraView;
            }

            public override void DidOutputSampleBuffer(
                AVCaptureOutput captureOutput,
                CMSampleBuffer sampleBuffer,
                AVCaptureConnection connection)
            {
                try
                {
                    // Skip frames to reduce load
                    if (_skipFrames > 0)
                    {
                        _skipFrames--;
                        sampleBuffer?.Dispose();
                        return;
                    }

                    if (_isProcessing)
                    {
                        sampleBuffer?.Dispose();
                        return;
                    }

                    _isProcessing = true;
                    _skipFrames = SKIP_FRAME_COUNT;
                    // Convert to UIImage
                    var image = GetImageFromSampleBuffer(sampleBuffer);
                    if (image != null)
                    {
                        // Convert to JPEG bytes
                        var jpegData = image.AsJPEG(0.85f);
                        if (jpegData != null)
                        {
                            var bytes = jpegData.ToArray();

                            var args = new CameraFrameEventArgs
                            {
                                ImageData = bytes,
                                Width = (int)image.Size.Width,
                                Height = (int)image.Size.Height,
                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                RotationDegrees = 0
                            };

                            // Raise event on main thread
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                _cameraView.OnFrameAnalyzed(args);
                            });
                        }

                        image.Dispose();
                    }

                    sampleBuffer?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Frame processing error: {ex.Message}");
                }
                finally
                {
                    _isProcessing = false;
                }
            }

            private UIImage GetImageFromSampleBuffer(CMSampleBuffer sampleBuffer)
            {
                try
                {
                    using var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
                    if (pixelBuffer == null) return null;

                    pixelBuffer.Lock(CVPixelBufferLock.ReadOnly);

                    var baseAddress = pixelBuffer.BaseAddress;
                    var bytesPerRow = (int)pixelBuffer.BytesPerRow;
                    var width = (int)pixelBuffer.Width;
                    var height = (int)pixelBuffer.Height;

                    var colorSpace = CGColorSpace.CreateDeviceRGB();

                    using var context = new CGBitmapContext(
                        baseAddress,
                        width,
                        height,
                        8,
                        bytesPerRow,
                        colorSpace,
                        CGImageAlphaInfo.PremultipliedFirst);

                    var cgImage = context.ToImage();
                    pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);

                    if (cgImage == null) return null;

                    // Handle rotation and mirroring
                    var image = new UIImage(cgImage);

                    // Mirror front camera
                    if (_cameraView.UseFrontCamera)
                    {
                        image = UIImageExtensions.FlipImage(image);
                    }

                    // Rotate based on device orientation
                    var orientation = UIDevice.CurrentDevice.Orientation;
                    image = RotateImage(image, orientation);

                    cgImage.Dispose();

                    return image;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GetImageFromSampleBuffer error: {ex.Message}");
                    return null;
                }
            }

            private UIImage RotateImage(UIImage image, UIDeviceOrientation orientation)
            {
                UIImageOrientation imageOrientation;

                switch (orientation)
                {
                    case UIDeviceOrientation.Portrait:
                        imageOrientation = UIImageOrientation.Right;
                        break;

                    case UIDeviceOrientation.PortraitUpsideDown:
                        imageOrientation = UIImageOrientation.Left;
                        break;

                    case UIDeviceOrientation.LandscapeLeft:
                        imageOrientation = _cameraView.UseFrontCamera
                            ? UIImageOrientation.Down
                            : UIImageOrientation.Up;
                        break;

                    case UIDeviceOrientation.LandscapeRight:
                        imageOrientation = _cameraView.UseFrontCamera
                            ? UIImageOrientation.Up
                            : UIImageOrientation.Down;
                        break;

                    default:
                        imageOrientation = UIImageOrientation.Right;
                        break;
                }

                return new UIImage(image.CGImage, image.CurrentScale, imageOrientation);
            }
        }
        private class PhotoCaptureDelegate : AVCapturePhotoCaptureDelegate
        {
            private readonly TaskCompletionSource<Stream> _tcs;
            private readonly ImageFormat _format;
            private readonly iOSCameraView _cameraView;
            public PhotoCaptureDelegate(iOSCameraView cameraView,
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
                if (error != null)
                {
                    _tcs.TrySetException(new NSErrorException(error));
                    return;
                }
                // 1️⃣ Lấy data từ photo
                NSData photoData = photo.FileDataRepresentation;
                if (photoData == null)
                {
                    throw new Exception("Failed to get photo data");
                }
                // 2️⃣ Convert thành UIImage
                var originalImage = UIImage.LoadFromData(photoData);
                if (originalImage == null)
                {
                    throw new Exception("Failed to decode image");
                }

                // 3️⃣ Áp dụng transform (rotate + mirror nếu front camera)
                var transformedImage = ApplyTransform(originalImage);

                // 4️⃣ Convert lại thành NSData theo format
                NSData finalData = ConvertImageToData(transformedImage, _format);

                // 5️⃣ Lưu vào temp file
                var path = CreateTempFile(_format);
                File.WriteAllBytes(path, finalData.ToArray());

                // 6️⃣ Return stream
                _tcs.TrySetResult(finalData.AsStream());

                // 7️⃣ Raise event
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _cameraView.OnPhotoSaved(path);
                });
            }

            /// <summary>
            /// Áp dụng transform: mirror (nếu front camera)
            /// Sử dụng UIGraphicsImageRenderer (Apple's recommended way)
            /// </summary>
            private UIImage ApplyTransform(UIImage originalImage)
            {
                try
                {
                    // Nếu không phải front camera, return ảnh gốc
                    if (!_cameraView.UseFrontCamera)
                    {
                        return originalImage;
                    }

                    // Flip ảnh nếu front camera
                    return UIImageExtensions.FlipImage(originalImage);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Transform error: {ex.Message}");
                    return originalImage;
                }
            }

            /// <summary>
            /// Convert UIImage thành NSData theo format
            /// </summary>
            private NSData ConvertImageToData(UIImage image, ImageFormat format)
            {
                if (format == ImageFormat.Png)
                {
                    return image.AsPNG();
                }
                else
                {
                    return image.AsJPEG(0.95f);
                }
            }

            /// <summary>
            /// Tạo temp file path
            /// </summary>
            public static string CreateTempFile(ImageFormat imageFormat)
            {
                var formatExt = imageFormat switch
                {
                    ImageFormat.Jpeg => ".jpg",
                    ImageFormat.Png => ".png",
                    _ => ".jpg"
                };

                // iOS temp directory (sandbox-safe)
                var tempDir = NSSearchPath.GetDirectories(NSSearchPathDirectory.CachesDirectory, NSSearchPathDomain.User)[0];

                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                var fileName = $"camera_{Guid.NewGuid():N}{formatExt}";
                return Path.Combine(tempDir, fileName);
            }
        }

        #endregion Frame Delegate
    }
}