using Mediapipe.Maui.Models;

namespace Mediapipe.Maui.Interfaces
{
    /// <summary>
    /// Factory for creating MediaPipe detectors
    /// </summary>
    public interface IMediaPipeDetectorFactory
    {
        IMediaPipeDetector<FaceLandmarksResult> CreateFaceLandmarker();

        IMediaPipeDetector<HandLandmarksResult> CreateHandLandmarker();

        IMediaPipeDetector<PoseLandmarksResult> CreatePoseLandmarker();

        IMediaPipeDetector<ObjectDetectionResult> CreateObjectDetector();
    }
}