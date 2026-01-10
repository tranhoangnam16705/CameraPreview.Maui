using CameraPreview.Maui.Controls;

namespace CameraPreview.Maui
{
    public static class Extensions
    {
        public static MauiAppBuilder UseCameraPreView(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers((handlers) =>
              {
#if ANDROID
                  handlers.AddHandler<CameraView, Platforms.Android.Handler.CameraViewHandler>();

#elif IOS
           handlers.AddHandler<CameraView, Platforms.iOS.Handler.CameraViewHandler>();
#endif
              });

            return builder;
        }
    }
}