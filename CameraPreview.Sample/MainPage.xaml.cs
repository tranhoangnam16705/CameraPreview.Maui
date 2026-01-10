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
        public MainPage(DrowsinessAnalyzer drowsiness,
            SmokingAnalyzer smoking,
            FaceMeshAnalyzer faceMeshAnalyzer)
        {
            InitializeComponent();
            _drowsiness = drowsiness;
            _smoking = smoking;
            _faceMeshAnalyzer = faceMeshAnalyzer;
        }


        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            Navigation.PushModalAsync(new NavigationPage(new CameraPage(_drowsiness, _smoking, _faceMeshAnalyzer)));

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}
