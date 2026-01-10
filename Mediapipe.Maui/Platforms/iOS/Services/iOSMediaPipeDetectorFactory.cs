using Mediapipe.Maui.Interfaces;
using Mediapipe.Maui.Models;

namespace Mediapipe.Maui.Platforms.iOS.Services
{
    /// <summary>
    /// iOS MediaPipe detector factory
    /// </summary>
    public class iOSMediaPipeDetectorFactory : IMediaPipeDetectorFactory
    {
        public IMediaPipeDetector<FaceLandmarksResult> CreateFaceLandmarker()
        {
            return new iOSFaceLandmarker();
        }

        public IMediaPipeDetector<HandLandmarksResult> CreateHandLandmarker()
        {
            return new iOSHandLandmarker();
        }

        public IMediaPipeDetector<PoseLandmarksResult> CreatePoseLandmarker()
        {
            // TODO: Implement pose landmarker
            throw new NotImplementedException("Pose landmarker not yet implemented");
        }

        public IMediaPipeDetector<ObjectDetectionResult> CreateObjectDetector()
        {
            // TODO: Implement object detector
            throw new NotImplementedException("Object detector not yet implemented");
        }
    }
}