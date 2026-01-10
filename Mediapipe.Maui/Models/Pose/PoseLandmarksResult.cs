using Mediapipe.Maui.Interfaces;

namespace Mediapipe.Maui.Models
{
    /// <summary>
    /// Pose landmarks detection result
    /// </summary>
    public class PoseLandmarksResult : IDetectionResult
    {
        public bool IsDetected { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public double ProcessingTimeMs { get; set; }
        public List<PoseLandmark> Landmarks { get; set; } = new();
        public float DetectionConfidence { get; set; }
    }
}