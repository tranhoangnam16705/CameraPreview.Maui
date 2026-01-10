using Android.Content;
using Mediapipe.Maui.Interfaces;
using Mediapipe.Maui.Models;

namespace Mediapipe.Maui.Platforms.Android.Services
{
    /// <summary>
    /// Android MediaPipe detector factory
    /// </summary>
    public class AndroidMediaPipeDetectorFactory : IMediaPipeDetectorFactory
    {
        private readonly Context _context;

        public AndroidMediaPipeDetectorFactory(Context context)
        {
            _context = context;
        }

        public IMediaPipeDetector<FaceLandmarksResult> CreateFaceLandmarker()
        {
            return new AndroidFaceLandmarker(_context);
        }

        public IMediaPipeDetector<HandLandmarksResult> CreateHandLandmarker()
        {
            return new AndroidHandLandmarker(_context);
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