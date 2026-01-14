using BarcodeScanner.Maui.Services;
using CameraPreview.Maui.Controls;
using CameraPreview.Maui.Models;

namespace CameraPreview.Sample;

public partial class ScanPage : ContentPage
{
    private readonly IBarcodeScanner _barcodeScanner;
    private volatile bool _isScaned;

    public ScanPage(IBarcodeScanner barcodeScanner)
    {
        InitializeComponent();
        _barcodeScanner = barcodeScanner;
    }

    private async void OnFrameReady(object sender, CameraFrameEventArgs e)
    {
        // Skip frame if already processing to prevent queue buildup
        if (_isScaned)
            return;
        try
        {
            var result = await _barcodeScanner.DecodeAsync(e.ImageData);
            if (result != null && result.Length > 0)
            {
                _isScaned = true;
                var mainpage = App.Current?.Windows?[0].Page;
                if (mainpage != null)
                {
                    await mainpage.DisplayAlertAsync("QUÉT QRCode thành công", result[0].Text, "Đóng");
                }
            }
        }
        finally
        {
            _isScaned = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        camerapreview.HandlerChanged += OnHandlerChanged;
        camerapreview.CameraStarted += Camerapreview_CameraStarted;
    }

    private async void Camerapreview_CameraStarted(object? sender, EventArgs e)
    {
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        camerapreview.HandlerChanged -= OnHandlerChanged;

        var camera = camerapreview.Cameras.FirstOrDefault(x => x.Position == CameraPreviewPosition.Back);
        if (camera != null)
        {
            camerapreview.Camera = camera;
        }
        MainThread.BeginInvokeOnMainThread(async () => await camerapreview.StartAsync());
    }
}