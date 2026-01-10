using Mediapipe.Maui.Interfaces;

namespace Mediapipe.Maui.Models
{
    /// <summary>
    /// Hand landmarks detection result
    /// </summary>
    public class HandLandmarksResult : IDetectionResult
    {
        public bool IsDetected { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public double ProcessingTimeMs { get; set; }
        public List<List<HandLandmark>> Hands { get; set; } = new();
        public List<HandType> Handedness { get; set; } = new(); // Left or Right
    }
}