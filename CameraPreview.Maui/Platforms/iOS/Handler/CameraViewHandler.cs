using CameraPreview.Maui.Controls;
using Microsoft.Maui.Handlers;
using System.Diagnostics;

namespace CameraPreview.Maui.Platforms.iOS.Handler
{
    /// <summary>
    /// MAUI Handler for iOS Camera
    /// </summary>
    public class CameraViewHandler : ViewHandler<CameraView, iOSCameraView>
    {
        public static IPropertyMapper<CameraView, CameraViewHandler> PropertyMapper =
            new PropertyMapper<CameraView, CameraViewHandler>(ViewHandler.ViewMapper)
            {
                [nameof(CameraView.Camera)] = MapCamera
            };

        public static CommandMapper<CameraView, CameraViewHandler> CommandMapper =
            new CommandMapper<CameraView, CameraViewHandler>(ViewHandler.ViewCommandMapper)
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

        protected override iOSCameraView CreatePlatformView()
        {
            Debug.WriteLine("Creating native iOS camera view");

            var iosCameraView = new iOSCameraView(VirtualView);

            // Wire up events
            iosCameraView.FrameReady += (sender, args) =>
            {
                VirtualView?.RaiseFrameReady(args);
            };

            iosCameraView.CameraStarted += (sender, args) =>
            {
                VirtualView?.RaiseCameraStarted();
            };

            iosCameraView.CameraStopped += (sender, args) =>
            {
                VirtualView?.RaiseCameraStopped();
            };

            iosCameraView.CameraError += (sender, error) =>
            {
                VirtualView?.RaiseCameraError(error);
            };

            return iosCameraView;
        }

        protected override void ConnectHandler(iOSCameraView platformView)
        {
            base.ConnectHandler(platformView);

            Debug.WriteLine("iOS CameraViewHandler connected");

            // Auto-start if specified
            if (VirtualView.AutoStart)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(500);
                    await platformView.StartAsync();
                });
            }
        }

        protected override void DisconnectHandler(iOSCameraView platformView)
        {
            Debug.WriteLine("iOS CameraViewHandler disconnecting");

            platformView.StopCamera();
            platformView.FrameReady -= OnFrameReady;
            platformView.CameraStarted -= OnCameraStarted;
            platformView.CameraStopped -= OnCameraStopped;
            platformView.CameraError -= OnCameraError;

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
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var result = await handler.PlatformView.TakePhotoAsync(req.Format);
                req.Completion.TrySetResult(result);
            }).Wait();
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

        private void PhotoSaved(object sender, string error)
        {
            VirtualView?.RaisePhotoSaved(error);
        }

        #endregion Event Handlers
    }
}