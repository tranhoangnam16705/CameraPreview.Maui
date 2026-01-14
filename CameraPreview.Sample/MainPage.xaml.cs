using BarcodeScanner.Maui.Services;
using Mediapipe.Maui.Services;
using Mediapipe.Maui.Servicesss;

namespace CameraPreview.Sample
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        private readonly DrowsinessAnalyzer _drowsiness;
        private readonly SmokingAnalyzer _smoking;
        private readonly FaceMeshAnalyzer _faceMeshAnalyzer;
        private readonly IBarcodeScanner _barcodeScanner;
        public MainPage(DrowsinessAnalyzer drowsiness,
            SmokingAnalyzer smoking,
            FaceMeshAnalyzer faceMeshAnalyzer, IBarcodeScanner barcodeScanner)
        {
            InitializeComponent();
            _drowsiness = drowsiness;
            _smoking = smoking;
            _faceMeshAnalyzer = faceMeshAnalyzer;
            _barcodeScanner = barcodeScanner;
        }


        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            Navigation.PushModalAsync(new NavigationPage(new CameraPage(_drowsiness, _smoking, _faceMeshAnalyzer)));

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private void OnScanClicked(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new NavigationPage(new ScanPage(_barcodeScanner)));
        }
    }
}
