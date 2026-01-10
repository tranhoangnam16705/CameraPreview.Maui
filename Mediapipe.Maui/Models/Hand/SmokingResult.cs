namespace Mediapipe.Maui.Models
{
    public class SmokingResult
    {
        public bool IsSmokingDetected { get; set; }
        public float HandToMouthDistance { get; set; }
        public HandType DetectedHand { get; set; }
        public int ConsecutiveFrames { get; set; }
        public TimeSpan Duration { get; set; }
    }
}