using CameraPreview.Maui.Controls;
using CameraPreview.Maui.Models;
using System.Threading.Tasks;

namespace CameraPreview.Sample;

public partial class CameraPage : ContentPage
{
    public CameraPage()
    {
        InitializeComponent();
    }

    private async void OnFrameReady(object sender, CameraFrameEventArgs e)
    {
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        camerapreview.HandlerChanged += OnHandlerChanged; ;
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        camerapreview.HandlerChanged -= OnHandlerChanged;

        var camera = camerapreview.Cameras.FirstOrDefault(x => x.Position == CameraPreviewPosition.Front);
        if (camera != null)
        {
            camerapreview.Camera = camera;
        }
        MainThread.BeginInvokeOnMainThread(async () => await camerapreview.StartAsync());
    }

    public ImageSource ToImageSource(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        // Quan trọng: reset position
        if (stream.CanSeek)
            stream.Position = 0;

        return ImageSource.FromStream(() =>
        {
            if (stream.CanSeek)
                stream.Position = 0;

            return stream;
        });
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var image = await camerapreview.TakePhotoAsync();
        imagesource.Source = ToImageSource(image);
    }
}