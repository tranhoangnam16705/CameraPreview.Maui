using CameraPreview.Maui;
using CameraPreview.Maui.Controls;
using Mediapipe.Maui.Services;
using Mediapipe.Maui.Servicesss;
using Microsoft.Extensions.Logging;
using Mediapipe.Maui;

namespace CameraPreview.Sample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                }).UseCameraPreView().UseMediaPipe();

            builder.Services.AddTransient<DrowsinessAnalyzer>();
            builder.Services.AddTransient<SmokingAnalyzer>();
            builder.Services.AddTransient<FaceMeshAnalyzer>();
            builder.Services.AddTransient<HandTrackingAnalyzer>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
