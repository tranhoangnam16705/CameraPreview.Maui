using Mediapipe.Maui.Interfaces;

namespace Mediapipe.Maui.Models
{
    /// <summary>
    /// Object detection result
    /// </summary>
    public class ObjectDetectionResult : IDetectionResult
    {
        public bool IsDetected { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public double ProcessingTimeMs { get; set; }
        public List<DetectedObject> Objects { get; set; } = new();
    }
}