using Mediapipe.Maui.Interfaces;
using Mediapipe.Maui.Models;
using System.Diagnostics;

namespace Mediapipe.Maui.Services
{
    /// <summary>
    /// Simple hand tracking (just landmarks)
    /// </summary>
    public class HandTrackingAnalyzer
    {
        private readonly IMediaPipeDetector<HandLandmarksResult> _handLandmarker;

        public HandTrackingAnalyzer(IMediaPipeDetector<HandLandmarksResult> handLandmarker)
        {
            _handLandmarker = handLandmarker ?? throw new ArgumentNullException(nameof(handLandmarker));
        }

        /// <summary>
        /// Get hand landmarks
        /// </summary>
        public async Task<HandTrackingResult> AnalyzeAsync(byte[] imageData)
        {
            var result = new HandTrackingResult();

            try
            {
                var handResult = await _handLandmarker.DetectAsync(imageData);

                if (handResult.IsDetected)
                {
                    result.IsDetected = true;
                    result.Hands = handResult.Hands;
                    result.Handedness = handResult.Handedness;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Hand tracking error: {ex.Message}");
            }

            return result;
        }
    }
}