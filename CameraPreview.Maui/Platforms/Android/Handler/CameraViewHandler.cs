using CameraPreview.Maui.Controls;
using Microsoft.Maui.Handlers;

namespace CameraPreview.Maui.Platforms.Android.Handler
{
    /// <summary>
    /// MAUI Handler that bridges CameraView (MAUI) to AndroidCameraView (Native)
    /// </summary>
    public partial class CameraViewHandler : ViewHandler<CameraView, AndroidCameraView>
    {
        public static IPropertyMapper<CameraView, CameraViewHandler> PropertyMapper = new PropertyMapper<CameraView, CameraViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(CameraView.Camera)] = MapCamera
        };

        public static CommandMapper<CameraView, CameraViewHandler> CommandMapper = new CommandMapper<CameraView, CameraViewHandler>(ViewHandler.ViewCommandMapper)
        {
            [nameof(CameraView.StartAsync)] = MapStartAsync,
            [nameof(CameraView.Stop)] = MapStop,
            [nameof(CameraView.GetSnapShotAsync)] = GetSnapShotAsync,
            [nameof(CameraView.SaveSnapShotAsync)] = SaveSnapShotAsync,
            [nameof(CameraView.TakePhotoAsync)] = TakePhotoAsync,
        };

        public CameraViewHandler() : base(PropertyMapper, CommandMapper)
        {
        }

        protected override AndroidCameraView CreatePlatformView()
        {
            System.Diagnostics.Debug.WriteLine("Creating native Android camera view");

            var context = Context ?? throw new InvalidOperationException("Context is null");
            var androidCameraView = new AndroidCameraView(context, VirtualView);

            // Wire up events from native to MAUI
            androidCameraView.FrameReady += (sender, args) =>
            {
                VirtualView?.RaiseFrameReady(args);
            };

            androidCameraView.CameraStarted += (sender, args) =>
            {
                VirtualView?.RaiseCameraStarted();
            };

            androidCameraView.CameraStopped += (sender, args) =>
            {
                VirtualView?.RaiseCameraStopped();
            };

            androidCameraView.CameraError += (sender, error) =>
            {
                VirtualView?.RaiseCameraError(error);
            };

            androidCameraView.TakePhotoSaved += (sender, error) =>
            {
                VirtualView?.RaisePhotoSaved(error);
            };
            return androidCameraView;
        }

        protected override void ConnectHandler(AndroidCameraView platformView)
        {
            base.ConnectHandler(platformView);

            System.Diagnostics.Debug.WriteLine("CameraViewHandler connected");

            // Auto-start if specified
            if (VirtualView.AutoStart)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(500); // Small delay to ensure everything is ready
                    await platformView.StartAsync();
                });
            }
        }

        protected override void DisconnectHandler(AndroidCameraView platformView)
        {
            System.Diagnostics.Debug.WriteLine("CameraViewHandler disconnecting");

            platformView.StopCamera();
            platformView.FrameReady -= OnFrameReady;
            platformView.CameraStarted -= OnCameraStarted;
            platformView.CameraStopped -= OnCameraStopped;
            platformView.CameraError -= OnCameraError;
            platformView.TakePhotoSaved -= OnPhotoSaved;

            base.DisconnectHandler(platformView);
        }

        #region Property Mappers

        private static void MapCamera(CameraViewHandler handler, CameraView view)
        {
            if (handler.PlatformView == null || view.Camera == null)
                return;

            handler.PlatformView.SetCamera(view.Camera);
        }

        #endregion Property Mappers

        #region Command Mappers

        private static async void MapStartAsync(CameraViewHandler handler, CameraView virtualView, object args)
        {
            if (handler.PlatformView != null)
            {
                await handler.PlatformView.StartAsync();
            }
        }

        private static void MapStop(CameraViewHandler handler, CameraView virtualView, object args)
        {
            if (handler.PlatformView != null)
            {
                handler.PlatformView.StopCamera();
            }
        }

        private static void GetSnapShotAsync(CameraViewHandler handler, CameraView view, object args)
        {
            if (handler.PlatformView == null || args is not SnapshotRequest req)
            {
                (args as SnapshotRequest)?.Completion.TrySetResult(null);
                return;
            }

            var image = handler.PlatformView.GetSnapShot(req.Format);
            if (image != null)
            {
                req.Completion.TrySetResult(image);
            }
        }

        private static void SaveSnapShotAsync(CameraViewHandler handler, CameraView view, object args)
        {
            if (handler.PlatformView == null || args is not SaveSnapshotRequest req)
            {
                (args as SaveSnapshotRequest)?.Completion.TrySetResult(false);
                return;
            }
            var result = handler.PlatformView.SaveSnapShot(req.Format, req.SnapFilePath);
            req.Completion.TrySetResult(result);
        }

        private static void TakePhotoAsync(CameraViewHandler handler, CameraView view, object args)
        {
            if (handler.PlatformView == null || args is not TakePhotoRequest req)
            {
                (args as TakePhotoRequest)?.Completion.TrySetResult(null);
                return;
            }
            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var result = await handler.PlatformView.TakePhotoAsync(req.Format);
                    req.Completion.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    req.Completion.TrySetException(ex);
                }
            });
        }

        #endregion Command Mappers

        #region Event Handlers

        private void OnFrameReady(object sender, CameraFrameEventArgs e)
        {
            VirtualView?.RaiseFrameReady(e);
        }

        private void OnCameraStarted(object sender, EventArgs e)
        {
            VirtualView?.RaiseCameraStarted();
        }

        private void OnCameraStopped(object sender, EventArgs e)
        {
            VirtualView?.RaiseCameraStopped();
        }

        private void OnCameraError(object sender, string error)
        {
            VirtualView?.RaiseCameraError(error);
        }

        private void OnPhotoSaved(object sender, string error)
        {
            VirtualView?.RaisePhotoSaved(error);
        }

        #endregion Event Handlers
    }
}