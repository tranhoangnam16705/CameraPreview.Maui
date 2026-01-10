namespace Mediapipe.Maui.Interfaces
{
    /// <summary>
    /// Base interface for detection results
    /// </summary>
    public interface IDetectionResult
    {
        bool IsDetected { get; set; }
        DateTime Timestamp { get; set; }
        double ProcessingTimeMs { get; set; }
    }
}