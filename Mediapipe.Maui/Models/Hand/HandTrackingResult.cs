namespace Mediapipe.Maui.Models
{
    public class HandTrackingResult
    {
        public bool IsDetected { get; set; }
        public List<List<HandLandmark>> Hands { get; set; } = new();
        public List<HandType> Handedness { get; set; } = new();
    }
}