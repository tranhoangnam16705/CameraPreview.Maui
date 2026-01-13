using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.Views;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using CameraPreview.Maui.Controls;
using CameraPreview.Maui.Models;
using CameraCharacteristics = Android.Hardware.Camera2.CameraCharacteristics;
using Class = Java.Lang.Class;
using ImageFormat = Microsoft.Maui.Graphics.ImageFormat;
using SizeF = Android.Util.SizeF;

namespace CameraPreview.Maui.Platforms.Android.Handler
{
    /// <summary>
    /// Native Android Camera implementation using CameraX
    /// </summary>
    public class AndroidCameraView : FrameLayout
    {
        private CameraView _cameraView;
        private PreviewView _previewView;
        private ProcessCameraProvider _cameraProvider;
        private ICamera _camera;  // Interface ICamera, không phải class Camera
        private AndroidX.Camera.Core.Preview _preview;
        private ImageAnalysis _imageAnalysis;
        private ImageCapture _imageCapture;
        private CameraFrameAnalyzer _frameAnalyzer;
        private CameraManager _cameraManager;
        private readonly Context _context;
        private CameraFrameEventArgs lastCameraFrame;
        private readonly object lockCapture = new();
        private bool _initiated = false;
        private bool _isRunning = false;
        private bool _useFrontCamera = false;

        // Keep strong reference to photo callback to prevent GC
        private PhotoSaveCallback _currentPhotoCallback;

        public event EventHandler<CameraFrameEventArgs> FrameReady;

        public event EventHandler CameraStarted;

        public event EventHandler CameraStopped;

        public event EventHandler<string> TakePhotoSaved;

        public event EventHandler<string> CameraError;

        public AndroidCameraView(Context context, CameraView cameraView) : base(context)
        {
            _cameraView = cameraView;
            _context = context;
            // Create PreviewView and add to this FrameLayout
            _previewView = new PreviewView(context)
            {
            };
            _previewView.SetImplementationMode(PreviewView.ImplementationMode.Performance);
            // Add PreviewView to container
            var layoutParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent);

            AddView(_previewView, layoutParams);

            System.Diagnostics.Debug.WriteLine("AndroidCameraView created");
            InitDevices();
        }

        private void InitDevices()
        {
            _cameraManager = (CameraManager)_context.GetSystemService(Context.CameraService);
            foreach (var id in _cameraManager.GetCameraIdList())
            {
                var cameraInfo = new CameraPreviewInfo { DeviceId = id, MinZoomFactor = 1 };
                var chars = _cameraManager.GetCameraCharacteristics(id);
                if ((int)(chars.Get(CameraCharacteristics.LensFacing) as Java.Lang.Number) == (int)LensFacing.Back)
                {
                    cameraInfo.Name = "Back Camera";
                    cameraInfo.Position = CameraPreviewPosition.Back;
                }
                else if ((int)(chars.Get(CameraCharacteristics.LensFacing) as Java.Lang.Number) == (int)LensFacing.Front)
                {
                    cameraInfo.Name = "Front Camera";
                    cameraInfo.Position = CameraPreviewPosition.Front;
                }
                else
                {
                    cameraInfo.Name = "Camera " + id;
                    cameraInfo.Position = CameraPreviewPosition.Unknow;
                }
                cameraInfo.MaxZoomFactor = (float)(chars.Get(CameraCharacteristics.ScalerAvailableMaxDigitalZoom) as Java.Lang.Number);
                cameraInfo.HasFlashUnit = (bool)(chars.Get(CameraCharacteristics.FlashInfoAvailable) as Java.Lang.Boolean);
                cameraInfo.AvailableResolutions = new();

                try
                {
                    float[] maxFocus = (float[])chars.Get(CameraCharacteristics.LensInfoAvailableFocalLengths);
                    SizeF size = (SizeF)chars.Get(CameraCharacteristics.SensorInfoPhysicalSize);
                    cameraInfo.HorizontalViewAngle = (float)(2 * Math.Atan(size.Width / (maxFocus[0] * 2)));
                    cameraInfo.VerticalViewAngle = (float)(2 * Math.Atan(size.Height / (maxFocus[0] * 2)));
                }
                catch { }
                try
                {
                    StreamConfigurationMap map = (StreamConfigurationMap)chars.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                    foreach (var s in map.GetOutputSizes(Class.FromType(typeof(ImageReader))))
                        cameraInfo.AvailableResolutions.Add(new(s.Width, s.Height));
                }
                catch
                {
                    if (cameraInfo.Position == CameraPreviewPosition.Back)
                        cameraInfo.AvailableResolutions.Add(new(1920, 1080));
                    cameraInfo.AvailableResolutions.Add(new(1280, 720));
                    cameraInfo.AvailableResolutions.Add(new(640, 480));
                    cameraInfo.AvailableResolutions.Add(new(352, 288));
                }
                _cameraView.Cameras.Add(cameraInfo);
            }

            _initiated = true;
            _cameraView.RefreshDevices();
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
                        // Restart camera with new direction
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
                if (_initiated)
                {
                    if (await CameraView.RequestPermissions())
                    {
                        StopCamera();
                        if (_cameraView.Camera != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Starting Android camera...");

                            var context = Context;
                            if (context == null)
                            {
                                RaiseError("Context is null");
                                return;
                            }

                            // Get camera provider
                            var cameraProviderFuture = ProcessCameraProvider.GetInstance(context);

                            await Task.Run(() =>
                            {
                                _cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();
                            });

                            if (_cameraProvider == null)
                            {
                                RaiseError("Failed to get camera provider");
                                return;
                            }

                            // Unbind all before rebinding
                            _cameraProvider.UnbindAll();
                            _useFrontCamera = _cameraView.Camera.Position == CameraPreviewPosition.Front;
                            // Select camera
                            var cameraSelector = _useFrontCamera
                                ? CameraSelector.DefaultFrontCamera
                                : CameraSelector.DefaultBackCamera;

                            // Setup Preview
                            _preview = new AndroidX.Camera.Core.Preview.Builder()
                                .Build();

                            var executor = ContextCompat.GetMainExecutor(context);
                            _preview.SetSurfaceProvider(executor, _previewView.SurfaceProvider);

                            var rotation = GetRotation(_previewView.Display.Rotation);
                            // Setup Image Analysis
                            _imageAnalysis = new ImageAnalysis.Builder()
                                .SetTargetRotation(rotation)
                                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                                .SetOutputImageFormat(ImageAnalysis.OutputImageFormatRgba8888)
                                .Build();

                            // Create frame analyzer
                            _frameAnalyzer = new CameraFrameAnalyzer(this);
                            _imageAnalysis.SetAnalyzer(
                                executor,
                                _frameAnalyzer);

                            // Setup Image Capture
                            _imageCapture = new ImageCapture.Builder()
                                            .SetCaptureMode(ImageCapture.CaptureModeMaximizeQuality)
                                            .SetTargetRotation(rotation)
                                            .Build();

                            // Get lifecycle owner
                            var activity = context as AndroidX.Lifecycle.ILifecycleOwner;
                            if (activity == null)
                            {
                                RaiseError("Context is not a LifecycleOwner");
                                return;
                            }

                            // Bind to lifecycle - trả về ICamera interface
                            _camera = _cameraProvider.BindToLifecycle(
                                activity,
                                cameraSelector,
                                _preview,
                                _imageAnalysis, _imageCapture);

                            _isRunning = true;

                            System.Diagnostics.Debug.WriteLine($"Camera started: {(_useFrontCamera ? "Front" : "Back")}");
                            CameraStarted?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Start camera error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                RaiseError($"Start failed: {ex.Message}");
            }
        }

        public void SetCamera(CameraPreviewInfo camera)
        {
            _cameraView.Camera = camera;
        }

        private static int GetRotation(SurfaceOrientation rotation)
        {
            switch (rotation)
            {
                case SurfaceOrientation.Rotation0:
                    return 0;

                case SurfaceOrientation.Rotation180:
                    return 180;

                case SurfaceOrientation.Rotation270:
                    return 270;

                case SurfaceOrientation.Rotation90:
                    return 90;

                default:
                    throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null);
            }
        }

        public void StopCamera()
        {
            try
            {
                _cameraProvider?.UnbindAll();

                _camera = null;
                _preview = null;
                _imageAnalysis = null;
                _frameAnalyzer = null;
                _isRunning = false;

                System.Diagnostics.Debug.WriteLine("Camera stopped");
                CameraStopped?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stop camera error: {ex.Message}");
            }
        }

        private void RaiseError(string message)
        {
            System.Diagnostics.Debug.WriteLine($"Camera error: {message}");
            CameraError?.Invoke(this, message);
        }

        internal void OnFrameAnalyzed(CameraFrameEventArgs args)
        {
            FrameReady?.Invoke(this, args);
            lastCameraFrame = args;
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
                            var bitmap = BitmapFactory.DecodeByteArray(lastCameraFrame.ImageData, 0, lastCameraFrame.ImageData.Length);
                            if (bitmap != null)
                            {
                                if (File.Exists(SnapFilePath)) File.Delete(SnapFilePath);
                                var iformat = imageFormat switch
                                {
                                    ImageFormat.Jpeg => Bitmap.CompressFormat.Jpeg,
                                    _ => Bitmap.CompressFormat.Png
                                };
                                using FileStream stream = new(SnapFilePath, FileMode.OpenOrCreate);
                                bitmap.Compress(iformat, 80, stream);
                                stream.Close();
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

        public Task<System.IO.Stream> TakePhotoAsync(ImageFormat imageFormat)
        {
            var tcs = new TaskCompletionSource<System.IO.Stream>();

            if (_imageCapture == null)
            {
                System.Diagnostics.Debug.WriteLine("TakePhotoAsync: ImageCapture is null");
                tcs.TrySetException(new InvalidOperationException("ImageCapture not initialized"));
                return tcs.Task;
            }

            if (!IsRunning)
            {
                System.Diagnostics.Debug.WriteLine("TakePhotoAsync: Camera not running");
                tcs.TrySetException(new InvalidOperationException("Camera not running"));
                return tcs.Task;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("TakePhotoAsync: Starting photo capture...");

                var url = CreateTempFile(imageFormat);
                // Keep strong reference to prevent GC
                _currentPhotoCallback = new PhotoSaveCallback(this, url.AbsolutePath, tcs);
                _imageCapture.TakePicture(
                    ContextCompat.GetMainExecutor(Context),
                    _currentPhotoCallback);

                System.Diagnostics.Debug.WriteLine("TakePhotoAsync: TakePicture called");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TakePhotoAsync error: {ex.Message}");
                tcs.TrySetException(ex);
                RaiseError($"Take photo error: {ex.Message}");
            }

            return tcs.Task;
        }

        private Java.IO.File CreateTempFile(ImageFormat imageFormat)
        {
            var context = Context;
            if (context == null)
                throw new InvalidOperationException("Context is null");

            var dir = context.CacheDir; // /data/data/your.app/cache
            var fileName = $"photo_{Java.Lang.JavaSystem.CurrentTimeMillis()}";
            var iformat = imageFormat switch
            {
                ImageFormat.Jpeg => ".jpg",
                _ => ".png"
            };
            return Java.IO.File.CreateTempFile(fileName, iformat, dir);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopCamera();
                _previewView?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Frame Analyzer

        private class CameraFrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
        {
            private readonly AndroidCameraView _cameraView;
            private bool _isProcessing = false;
            private int _skipFrames = 0;
            private const int SKIP_FRAME_COUNT = 2; // Process every 3rd frame

            public CameraFrameAnalyzer(AndroidCameraView cameraView)
            {
                _cameraView = cameraView;
            }

            public void Analyze(IImageProxy imageProxy)
            {
                try
                {
                    // Skip frames to reduce load
                    if (_skipFrames > 0)
                    {
                        _skipFrames--;
                        imageProxy.Close();
                        return;
                    }

                    if (_isProcessing)
                    {
                        imageProxy.Close();
                        return;
                    }

                    _isProcessing = true;
                    _skipFrames = SKIP_FRAME_COUNT;

                    // Convert to bitmap
                    var bitmap = ImageProxyToBitmap(imageProxy);
                    if (bitmap != null)
                    {
                        var bytes = BitmapToJpegByteArray(bitmap);

                        var args = new CameraFrameEventArgs
                        {
                            ImageData = bytes,
                            Width = bitmap.Width,
                            Height = bitmap.Height,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            RotationDegrees = imageProxy.ImageInfo.RotationDegrees
                        };

                        // Raise event on main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _cameraView.OnFrameAnalyzed(args);
                        });

                        bitmap.Recycle();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Frame analysis error: {ex.Message}");
                }
                finally
                {
                    imageProxy.Close();
                    _isProcessing = false;
                }
            }

            private Bitmap ImageProxyToBitmap(IImageProxy imageProxy)
            {
                try
                {
                    var plane = imageProxy.GetPlanes()[0];
                    var buffer = plane.Buffer;

                    if (buffer == null) return null;

                    var bytes = new byte[buffer.Remaining()];
                    buffer.Get(bytes);

                    // 1️⃣ Bitmap gốc
                    var bitmap = Bitmap.CreateBitmap(
                        imageProxy.Width,
                        imageProxy.Height,
                        Bitmap.Config.Argb8888!);

                    bitmap.CopyPixelsFromBuffer(Java.Nio.ByteBuffer.Wrap(bytes));

                    var matrix = new Matrix();

                    // 1️⃣ Rotate theo camera
                    matrix.PostRotate(imageProxy.ImageInfo.RotationDegrees);

                    // 2️⃣ Mirror nếu front camera (KHÔNG dùng pivot width/height)
                    if (_cameraView.UseFrontCamera)
                    {
                        matrix.PostScale(-1f, 1f);
                    }

                    // 2️⃣ Apply rotate + mirror
                    var rotatedBitmap = Bitmap.CreateBitmap(
                        bitmap,
                        0,
                        0,
                        bitmap.Width,
                        bitmap.Height,
                        matrix,
                        true);

                    bitmap.Recycle(); // giải phóng bitmap gốc

                    return rotatedBitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ImageProxy to Bitmap error: {ex.Message}");
                    return null;
                }
            }

            private byte[] BitmapToJpegByteArray(Bitmap bitmap)
            {
                using var stream = new MemoryStream();
                bitmap.Compress(Bitmap.CompressFormat.Jpeg!, 85, stream);
                return stream.ToArray();
            }
        }

        private class PhotoSaveCallback : global::AndroidX.Camera.Core.ImageCapture.OnImageCapturedCallback
        {
            private readonly TaskCompletionSource<System.IO.Stream> _tcs;
            private readonly AndroidCameraView _cameraView;
            private readonly string _path;

            public PhotoSaveCallback(AndroidCameraView cameraView, string path, TaskCompletionSource<System.IO.Stream> tcs)
            {
                _tcs = tcs;
                _path = path;
                _cameraView = cameraView;
            }

            public override void OnCaptureSuccess(global::AndroidX.Camera.Core.IImageProxy image)
            {
                System.Diagnostics.Debug.WriteLine("PhotoSaveCallback: OnCaptureSuccess called");

                try
                {
                    var buffer = image.GetPlanes()[0].Buffer;
                    var bytes = new byte[buffer.Remaining()];
                    buffer.Get(bytes);

                    // Create bitmap from bytes
                    var bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
                    if (bitmap == null)
                    {
                        System.Diagnostics.Debug.WriteLine("PhotoSaveCallback: Failed to decode bitmap");
                        _tcs.TrySetException(new Exception("Failed to decode bitmap from bytes"));
                        return;
                    }

                    // Transform if needed (rotation + mirror for front camera)
                    var transformedBitmap = TransformBitmap(bitmap, image.ImageInfo.RotationDegrees);
                    bitmap.Recycle();

                    // Convert to stream
                    var stream = new MemoryStream();
                    transformedBitmap.Compress(Bitmap.CompressFormat.Jpeg!, 95, stream);
                    stream.Position = 0;

                    // Save to file
                    using (var fileStream = new FileStream(_path, FileMode.Create))
                    {
                        transformedBitmap.Compress(Bitmap.CompressFormat.Jpeg!, 95, fileStream);
                    }

                    transformedBitmap.Recycle();

                    System.Diagnostics.Debug.WriteLine($"PhotoSaveCallback: Photo saved to {_path}");

                    _tcs.TrySetResult(stream);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _cameraView.OnPhotoSaved(_path);
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PhotoSaveCallback error: {ex.Message}");
                    _tcs.TrySetException(ex);
                }
                finally
                {
                    image.Close();
                }
            }

            public override void OnError(global::AndroidX.Camera.Core.ImageCaptureException exception)
            {
                System.Diagnostics.Debug.WriteLine($"PhotoSaveCallback: OnError - {exception.Message}");
                _tcs.TrySetException(new Exception(exception.Message));
            }

            private Bitmap TransformBitmap(Bitmap originalBitmap, int rotationDegrees)
            {
                var matrix = new Matrix();

                // Rotate
                if (rotationDegrees != 0)
                {
                    matrix.PostRotate(rotationDegrees);
                }

                // Mirror for front camera
                if (_cameraView.UseFrontCamera)
                {
                    matrix.PostScale(-1f, 1f, originalBitmap.Width / 2f, 0);
                }

                var transformedBitmap = Bitmap.CreateBitmap(
                    originalBitmap,
                    0,
                    0,
                    originalBitmap.Width,
                    originalBitmap.Height,
                    matrix,
                    true);

                matrix.Dispose();
                return transformedBitmap;
            }
        }

        #endregion Frame Analyzer
    }
}