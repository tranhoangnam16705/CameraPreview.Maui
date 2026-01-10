using CameraPreview.Maui.Controls;
using CameraPreview.Maui.Models;
using Mediapipe.Maui.Controls;
using Mediapipe.Maui.Models;
using Mediapipe.Maui.Services;
using Mediapipe.Maui.Servicesss;
using System.Threading.Tasks;

namespace CameraPreview.Sample;

public partial class CameraPage : ContentPage
{
    private readonly DrowsinessAnalyzer _drowsiness;
    private readonly SmokingAnalyzer _smoking;
    private readonly FaceMeshAnalyzer _faceMeshAnalyzer;
    private readonly FaceMeshDrawable _overlayDrawable;

    public CameraPage(DrowsinessAnalyzer drowsiness,
        SmokingAnalyzer smoking,
        FaceMeshAnalyzer faceMeshAnalyzer)
    {
        InitializeComponent();
        _drowsiness = drowsiness;
        _smoking = smoking;
        _faceMeshAnalyzer = faceMeshAnalyzer;
        // Đăng ký sự kiện cảnh báo
        //_drowsiness.DrowsinessDetected += OnDrowsinessDetected;
        //_smoking.SmokingDetected += OnSmokingDetected;


        _overlayDrawable = new FaceMeshDrawable();
        OverlayCanvas.Drawable = _overlayDrawable;

    }

    private async void OnFrameReady(object sender, CameraFrameEventArgs e)
    {
        // Process with MediaPipe
        var result = await _faceMeshAnalyzer.AnalyzeAsync(e.ImageData);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // 2️⃣ TÍNH SCALE FACTOR Ở ĐÂY
            var scale = CalculateScaleFactor(
                viewWidth: (float)camerapreview.Width,
                viewHeight: (float)camerapreview.Height,
                imageWidth: e.Width,
                imageHeight: e.Height,
                runningMode: FaceRunningMode.LiveStream);

            _overlayDrawable.Results = result;
            _overlayDrawable.ImageWidth = (float)camerapreview.Width;
            _overlayDrawable.ImageHeight = (float)camerapreview.Height;
            _overlayDrawable.ScaleFactor = scale;
            OverlayCanvas.Invalidate();

            //UpdateUI(result);
        });
    }

    private float CalculateScaleFactor(
   float viewWidth,
   float viewHeight,
   int imageWidth,
   int imageHeight,
   FaceRunningMode runningMode)
    {
        return runningMode switch
        {
            FaceRunningMode.Image or FaceRunningMode.Video
                => Math.Min(viewWidth / imageWidth, viewHeight / imageHeight),

            FaceRunningMode.LiveStream
                => Math.Max(viewWidth / imageWidth, viewHeight / imageHeight),

            _ => 1f
        };
    }

    private void UpdateUI(FaceLandmarksResult result)
    {
        //if (result.IsDetected)
        //{
        //    lblEAR.Text = $"EAR: {result.EyeAspectRatio:F3}";
        //    lblDrowsy.Text = result.IsDrowsy
        //        ? $"⚠️ BUỒN NGỦ ({result.DrowsyFrameCount} frames)"
        //        : "Mắt mở";

        //    lblSmoking.Text = result.IsSmokingDetected
        //        ? "🚭 ĐANG HÚT THUỐC"
        //        : "Không phát hiện hút thuốc";

        //    if (!result.IsDrowsy && !result.IsSmokingDetected)
        //    {
        //        lblStatus.Text = "Đang phân tích...";
        //    }
        //}
        //else
        //{
        //    lblEAR.Text = "EAR: - (Không thấy mặt)";
        //    lblDrowsy.Text = "Không phát hiện khuôn mặt";
        //    lblSmoking.Text = "Không phát hiện khuôn mặt";
        //}
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        camerapreview.HandlerChanged += OnHandlerChanged;
        camerapreview.CameraStarted += Camerapreview_CameraStarted;
    }

    private async void Camerapreview_CameraStarted(object? sender, EventArgs e)
    {
        var option = new MediaPipeOptions();
        option.MaxNumResults = 3;
        await _faceMeshAnalyzer.InitializeAsync(option);
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
        var image = await camerapreview.GetSnapShotAsync();
        imagesource.Source = image;
    }
}