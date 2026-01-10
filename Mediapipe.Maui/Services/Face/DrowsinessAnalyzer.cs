using Mediapipe.Maui.Interfaces;
using Mediapipe.Maui.Models;
using System.Diagnostics;

namespace Mediapipe.Maui.Servicesss
{
    /// <summary>
    /// Drowsiness detection using Face Landmarker
    /// </summary>
    public class DrowsinessAnalyzer
    {
        private readonly IMediaPipeDetector<FaceLandmarksResult> _faceLandmarker;

        // Configuration
        public float EarThreshold { get; set; } = 0.25f;

        public int ConsecutiveFrameThreshold { get; set; } = 15;

        // State tracking
        private int _drowsyFrameCount = 0;

        private DateTime _drowsinessStartTime = DateTime.Now;
        private bool _isDrowsinessActive = false;

        // Events
        public event EventHandler<DrowsinessDetectedEventArgs> DrowsinessDetected;

        public DrowsinessAnalyzer(IMediaPipeDetector<FaceLandmarksResult> faceLandmarker)
        {
            _faceLandmarker = faceLandmarker ?? throw new ArgumentNullException(nameof(faceLandmarker));
        }

        /// <summary>
        /// Analyze frame for drowsiness
        /// </summary>
        public async Task<DrowsinessResult> AnalyzeAsync(byte[] imageData)
        {
            var result = new DrowsinessResult();

            try
            {
                // Detect face landmarks
                var faceResult = await _faceLandmarker.DetectAsync(imageData);

                if (faceResult.IsDetected && faceResult.Faces.Count > 0)
                {
                    foreach (var faceLandmark in faceResult.Faces)
                    {
                        // Calculate EAR
                        result.LeftEyeAspectRatio = CalculateEyeAR(faceLandmark, LeftEyeIndices);
                        result.RightEyeAspectRatio = CalculateEyeAR(faceLandmark, RightEyeIndices);
                        result.AverageEyeAspectRatio = (result.LeftEyeAspectRatio + result.RightEyeAspectRatio) / 2.0f;

                        // Check drowsiness
                        CheckDrowsiness(result);
                    }
                }
                else
                {
                    // Reset when no face detected
                    ResetState();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Drowsiness analysis error: {ex.Message}");
            }

            return result;
        }

        private void CheckDrowsiness(DrowsinessResult result)
        {
            if (result.AverageEyeAspectRatio < EarThreshold)
            {
                _drowsyFrameCount++;

                if (!_isDrowsinessActive)
                {
                    _drowsinessStartTime = DateTime.Now;
                }

                if (_drowsyFrameCount >= ConsecutiveFrameThreshold)
                {
                    result.IsDrowsy = true;
                    result.ConsecutiveFrames = _drowsyFrameCount;
                    result.Duration = DateTime.Now - _drowsinessStartTime;
                    _isDrowsinessActive = true;

                    var eventArgs = new DrowsinessDetectedEventArgs
                    {
                        EyeAspectRatio = result.AverageEyeAspectRatio,
                        LeftEyeAspectRatio = result.LeftEyeAspectRatio,
                        RightEyeAspectRatio = result.RightEyeAspectRatio,
                        ConsecutiveFrames = _drowsyFrameCount,
                        Duration = result.Duration,
                        DetectedAt = DateTime.Now,
                        EyesClosed = result.AverageEyeAspectRatio < 0.15f,
                        Reason = result.AverageEyeAspectRatio < 0.15f ?
                            "Eyes closed" :
                            "Low eye aspect ratio"
                    };

                    // Fire event
                    DrowsinessDetected?.Invoke(this, eventArgs);

                    Debug.WriteLine($"⚠️ DROWSINESS: EAR={result.AverageEyeAspectRatio:F3}, Frames={_drowsyFrameCount}");
                }
            }
            else
            {
                ResetState();
            }
        }

        private void ResetState()
        {
            _drowsyFrameCount = 0;
            _isDrowsinessActive = false;
        }

        private float CalculateEyeAR(List<FaceLandmark> landmarks, int[] eyePoints)
        {
            if (landmarks.Count < eyePoints.Max() + 1)
                return 0f;

            var p1 = landmarks[eyePoints[0]];
            var p2 = landmarks[eyePoints[1]];
            var p3 = landmarks[eyePoints[2]];
            var p4 = landmarks[eyePoints[3]];
            var p5 = landmarks[eyePoints[4]];
            var p6 = landmarks[eyePoints[5]];

            float vertical1 = p2.DistanceTo(p6);
            float vertical2 = p3.DistanceTo(p5);
            float horizontal = p1.DistanceTo(p4);

            if (horizontal < 0.0001f) return 0f;

            return (vertical1 + vertical2) / (2.0f * horizontal);
        }

        private static readonly int[] LeftEyeIndices = { 33, 160, 158, 133, 153, 144 };
        private static readonly int[] RightEyeIndices = { 362, 385, 387, 263, 373, 380 };
    }
}