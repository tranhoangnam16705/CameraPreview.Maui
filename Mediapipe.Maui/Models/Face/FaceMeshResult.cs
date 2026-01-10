namespace Mediapipe.Maui.Models
{
    public class FaceMeshResult
    {
        public bool IsDetected { get; set; }
        public List<List<FaceLandmark>> Landmarks { get; set; } = new();
        public float Confidence { get; set; }
    }
}