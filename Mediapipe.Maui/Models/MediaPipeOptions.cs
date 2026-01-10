namespace Mediapipe.Maui.Models
{
    /// <summary>
    /// MediaPipe detector options
    /// </summary>
    public class MediaPipeOptions
    {
        public float MinDetectionConfidence { get; set; } = 0.5f;
        public float MinTrackingConfidence { get; set; } = 0.5f;
        public int MaxNumResults { get; set; } = 1;
        public bool EnableFaceBlendshapes { get; set; } = false;
        public bool EnableFacialTransformationMatrix { get; set; } = false;
    }
}