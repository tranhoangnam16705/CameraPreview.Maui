using BarcodeScanner.Maui.Services;
using CameraPreview.Maui.Controls;
using CameraPreview.Maui.Models;

namespace CameraPreview.Sample;

public partial class ScanPage : ContentPage
{
    private readonly IBarcodeScanner _barcodeScanner;
    private volatile bool _isScaned;
    private volatile bool _isProcessing;

    public ScanPage(IBarcodeScanner barcodeScanner)
    {
        InitializeComponent();
        _barcodeScanner = barcodeScanner;
    }

    private async void OnFrameReady(object sender, CameraFrameEventArgs e)
    {
        if (_isScaned || _isProcessing)
            return;

        _isProcessing = true;
        try
        {
            var result = await _barcodeScanner.DecodeAsync(e.ImageData).ConfigureAwait(false);

            if (result != null && result.Length > 0)
            {
                _isScaned = true;
                var resultText = result[0].Text;

                // Stop camera before showing alert to prevent iOS dispatch queue issues
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        camerapreview.Stop();
                        await DisplayAlertAsync("QUÉT QRCode thành công", resultText, "Đóng");
                        await camerapreview.StartAsync();
                        _isScaned = false;
                    }
                    catch
                    {
                        _isScaned = false;
                        try { await camerapreview.StartAsync(); } catch { }
                    }
                });
            }
        }
        finally
        {
            _isProcessing = false;
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