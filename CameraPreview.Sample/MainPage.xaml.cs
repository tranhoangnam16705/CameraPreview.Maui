namespace CameraPreview.Sample
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            Navigation.PushModalAsync(new NavigationPage(new CameraPage()));

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}
