using Mediapipe.Maui.Interfaces;

namespace Mediapipe.Maui.Models
{
    /// <summary>
    /// Face landmarks detection result
    /// </summary>
    public class FaceLandmarksResult : IDetectionResult
    {
        public bool IsDetected { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public double ProcessingTimeMs { get; set; }
        public List<List<FaceLandmark>> Faces { get; set; } = new();
        public float DetectionConfidence { get; set; }
    }
}