using Mediapipe.Maui.Interfaces;
using Mediapipe.Maui.Models;

namespace Mediapipe.Maui
{
    public static class Extensions
    {
        public static MauiAppBuilder UseMediaPipe(this MauiAppBuilder builder)
        {
#if ANDROID
            // Register Android MediaPipe Factory
            builder.Services.AddSingleton<IMediaPipeDetectorFactory>(sp =>
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
                    ?? throw new InvalidOperationException("Activity not available");
                return new Mediapipe.Maui.Platforms.Android.Services.AndroidMediaPipeDetectorFactory(context);
            });

#elif IOS
           // Register Android MediaPipe Factory
            builder.Services.AddSingleton<IMediaPipeDetectorFactory>(sp =>
            {
                return new Mediapipe.Maui.Platforms.iOS.Services.iOSMediaPipeDetectorFactory();
            });
#endif
            // Register Face Landmarker
            builder.Services.AddSingleton<IMediaPipeDetector<FaceLandmarksResult>>(sp =>
            {
                var factory = sp.GetRequiredService<IMediaPipeDetectorFactory>();
                return factory.CreateFaceLandmarker();
            });

            // Register Hand Landmarker
            builder.Services.AddSingleton<IMediaPipeDetector<HandLandmarksResult>>(sp =>
            {
                var factory = sp.GetRequiredService<IMediaPipeDetectorFactory>();
                return factory.CreateHandLandmarker();
            });
            return builder;
        }
    }
}