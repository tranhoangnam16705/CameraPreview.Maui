using Mediapipe.Maui.Interfaces;
using Mediapipe.Maui.Models;
using System.Diagnostics;

namespace Mediapipe.Maui.Services
{
    /// <summary>
    /// Simple face mesh detection (just landmarks)
    /// </summary>
    public class FaceMeshAnalyzer
    {
        private readonly IMediaPipeDetector<FaceLandmarksResult> _faceLandmarker;

        public FaceMeshAnalyzer(IMediaPipeDetector<FaceLandmarksResult> faceLandmarker)
        {
            _faceLandmarker = faceLandmarker ?? throw new ArgumentNullException(nameof(faceLandmarker));
        }

        public async Task InitializeAsync(MediaPipeOptions options)
        {
            await _faceLandmarker.InitializeAsync(options);
        }

        /// <summary>
        /// Get face landmarks
        /// </summary>
        public async Task<FaceMeshResult> AnalyzeAsync(byte[] imageData)
        {
            var result = new FaceMeshResult();

            try
            {
                var faceResult = await _faceLandmarker.DetectAsync(imageData);

                if (faceResult.IsDetected && faceResult.Faces.Count > 0)
                {
                    result.IsDetected = true;
                    result.Landmarks = faceResult.Faces;
                    result.Confidence = faceResult.DetectionConfidence;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Face mesh analysis error: {ex.Message}");
            }

            return result;
        }
    }
}