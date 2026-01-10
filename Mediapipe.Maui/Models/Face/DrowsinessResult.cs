namespace Mediapipe.Maui.Models
{
    public class DrowsinessResult
    {
        public bool IsDrowsy { get; set; }
        public float LeftEyeAspectRatio { get; set; }
        public float RightEyeAspectRatio { get; set; }
        public float AverageEyeAspectRatio { get; set; }
        public int ConsecutiveFrames { get; set; }
        public TimeSpan Duration { get; set; }
    }
}