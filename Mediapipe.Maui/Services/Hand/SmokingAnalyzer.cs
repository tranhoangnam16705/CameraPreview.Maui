using Mediapipe.Maui.Interfaces;
using Mediapipe.Maui.Models;
using System.Diagnostics;

namespace Mediapipe.Maui.Services
{
    /// <summary>
    /// Smoking detection using Face + Hand Landmarkers
    /// </summary>
    public class SmokingAnalyzer
    {
        private readonly IMediaPipeDetector<FaceLandmarksResult> _faceLandmarker;
        private readonly IMediaPipeDetector<HandLandmarksResult> _handLandmarker;

        // Configuration
        public float HandToMouthDistanceThreshold { get; set; } = 0.15f;

        public int ConsecutiveFrameThreshold { get; set; } = 10;

        // State tracking
        private int _smokingFrameCount = 0;

        private DateTime _smokingStartTime = DateTime.Now;
        private bool _isSmokingActive = false;

        // Events
        public event EventHandler<SmokingDetectedEventArgs> SmokingDetected;

        public SmokingAnalyzer(
            IMediaPipeDetector<FaceLandmarksResult> faceLandmarker,
            IMediaPipeDetector<HandLandmarksResult> handLandmarker)
        {
            _faceLandmarker = faceLandmarker ?? throw new ArgumentNullException(nameof(faceLandmarker));
            _handLandmarker = handLandmarker ?? throw new ArgumentNullException(nameof(handLandmarker));
        }

        /// <summary>
        /// Analyze frame for smoking gesture
        /// </summary>
        public async Task<SmokingResult> AnalyzeAsync(byte[] imageData)
        {
            var result = new SmokingResult();

            try
            {
                // Detect face
                var faceResult = await _faceLandmarker.DetectAsync(imageData);
                if (!faceResult.IsDetected || faceResult.Faces.Count == 0)
                {
                    ResetState();
                    return result;
                }

                // Detect hands
                var handResult = await _handLandmarker.DetectAsync(imageData);
                if (!handResult.IsDetected || handResult.Hands.Count == 0)
                {
                    ResetState();
                    return result;
                }

                // Analyze hand-to-mouth proximity
                var faceLandmarks = faceResult.Faces[0];
                var mouthLandmark = faceLandmarks[13]; // Upper lip center

                float minDistance = float.MaxValue;
                HandType detectedHand = HandType.Unknown;

                for (int i = 0; i < handResult.Hands.Count; i++)
                {
                    var hand = handResult.Hands[i];
                    var indexTip = hand.FirstOrDefault(l => l.Index == 8);
                    var middleTip = hand.FirstOrDefault(l => l.Index == 12);

                    if (indexTip != null && middleTip != null)
                    {
                        float distance = CalculateDistance(indexTip, middleTip, mouthLandmark);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            detectedHand = i < handResult.Handedness.Count ?
                                handResult.Handedness[i] : HandType.Unknown;
                        }
                    }
                }

                result.HandToMouthDistance = minDistance;
                result.DetectedHand = detectedHand;

                // Check if smoking
                if (minDistance < HandToMouthDistanceThreshold)
                {
                    _smokingFrameCount++;

                    if (!_isSmokingActive)
                    {
                        _smokingStartTime = DateTime.Now;
                    }

                    if (_smokingFrameCount >= ConsecutiveFrameThreshold)
                    {
                        result.IsSmokingDetected = true;
                        result.ConsecutiveFrames = _smokingFrameCount;
                        result.Duration = DateTime.Now - _smokingStartTime;
                        _isSmokingActive = true;

                        // Fire event
                        SmokingDetected?.Invoke(this, new SmokingDetectedEventArgs
                        {
                            HandToMouthDistance = minDistance,
                            DetectedHand = detectedHand,
                            ConsecutiveFrames = _smokingFrameCount,
                            Duration = result.Duration,
                            GestureType = SmokingGestureType.Confirmed
                        });

                        Debug.WriteLine($"🚭 SMOKING: Distance={minDistance:F3}, Hand={detectedHand}");
                    }
                }
                else
                {
                    ResetState();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Smoking analysis error: {ex.Message}");
            }

            return result;
        }

        private void ResetState()
        {
            _smokingFrameCount = 0;
            _isSmokingActive = false;
        }

        private float CalculateDistance(HandLandmark p1, HandLandmark p2, FaceLandmark mouth)
        {
            float avgX = (p1.X + p2.X) / 2f;
            float avgY = (p1.Y + p2.Y) / 2f;
            float avgZ = (p1.Z + p2.Z) / 2f;

            float dx = avgX - mouth.X;
            float dy = avgY - mouth.Y;
            float dz = avgZ - mouth.Z;

            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}