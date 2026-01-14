using BarcodeScanner.Maui.Services;

namespace BarcodeScanner.Maui
{
    public static class Extensions
    {
        public static MauiAppBuilder UseBarcodeScanner(this MauiAppBuilder builder)
        {
#if ANDROID
            builder.Services.AddSingleton<IBarcodeScanner, Platforms.Android.Services.AndroidBarcodeScanner>();
#elif IOS
            builder.Services.AddSingleton<IBarcodeScanner, Platforms.iOS.Services.iOSBarcodeScanner>();
#endif
            return builder;
        }
    }
}
