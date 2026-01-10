using CoreGraphics;
using CoreImage;
using Foundation;
using Mediapipe.Maui.Models;
using Mediapipe.Maui.Services;
using System.Diagnostics;
using Vision;

namespace Mediapipe.Maui.Platforms.iOS.Services
{
    /// <summary>
    /// iOS Hand Landmarker using Apple Vision Framework
    /// Vision provides 21 hand landmarks (same as MediaPipe)
    /// </summary>
    public class iOSHandLandmarker : MediaPipeDetectorBase<HandLandmarksResult>
    {
        private VNDetectHumanHandPoseRequest _handPoseRequest;
        private VNSequenceRequestHandler _requestHandler;

        public override string DetectorName => "iOS Hand Landmarker (Vision)";

        protected override Task InitializeDetectorAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    // Create hand pose detection request
                    _handPoseRequest = new VNDetectHumanHandPoseRequest(HandleHandDetection)
                    {
                        MaximumHandCount = (nuint)_options.MaxNumResults,
                        // Use accurate mode
                        Revision = VNDetectHumanHandPoseRequestRevision.One
                    };

                    _requestHandler = new VNSequenceRequestHandler();

                    Debug.WriteLine("iOSHandLandmarker initialized with Vision Framework");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"iOSHandLandmarker init error: {ex.Message}");
                    throw;
                }
            });
        }

        protected override Task<HandLandmarksResult> PerformDetectionAsync(byte[] imageData)
        {
            return Task.Run(() =>
            {
                var result = new HandLandmarksResult();

                try
                {
                    if (_handPoseRequest == null || _requestHandler == null)
                        return result;

                    // Convert byte[] to CGImage
                    using var dataProvider = new CGDataProvider(imageData, 0, imageData.Length);
                    using var cgImage = CGImage.FromJPEG(dataProvider, null, false, CGColorRenderingIntent.Default);

                    if (cgImage == null)
                    {
                        Debug.WriteLine("Failed to create CGImage from data");
                        return result;
                    }

                    // Create CIImage for Vision
                    using var ciImage = new CIImage(cgImage);

                    // Perform detection
                    NSError error;
                    _requestHandler.Perform(new[] { _handPoseRequest }, ciImage, out error);

                    if (error != null)
                    {
                        Debug.WriteLine($"Vision hand request error: {error.LocalizedDescription}");
                        return result;
                    }

                    // Get results
                    var observations = _handPoseRequest.GetResults<VNHumanHandPoseObservation>();

                    if (observations != null && observations.Length > 0)
                    {
                        result.IsDetected = true;

                        // Process each detected hand
                        foreach (var observation in observations)
                        {
                            var handLandmarks = ConvertVisionHandLandmarks(observation);
                            var handedness = DetermineHandedness(observation);

                            result.Hands.Add(handLandmarks);
                            result.Handedness.Add(handedness);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"iOS Hand detection error: {ex.Message}");
                }

                return result;
            });
        }

        private void HandleHandDetection(VNRequest request, NSError error)
        {
            if (error != null)
            {
                Debug.WriteLine($"Hand detection callback error: {error.LocalizedDescription}");
            }
        }

        /// <summary>
        /// Convert Vision's hand landmarks to MediaPipe format
        /// Vision and MediaPipe both use 21 landmarks with same topology
        /// </summary>
        private List<HandLandmark> ConvertVisionHandLandmarks(VNHumanHandPoseObservation observation)
        {
            var landmarks = new List<HandLandmark>();

            try
            {
                // Vision hand landmarks mapping to MediaPipe indices
                var landmarkMap = new Dictionary<VNHumanHandPoseObservationJointName, int>
                {
                    // Wrist
                    { VNHumanHandPoseObservationJointName.Wrist, 0 },

                    // Thumb
                    { VNHumanHandPoseObservationJointName.ThumbCmc, 1 },
                    { VNHumanHandPoseObservationJointName.ThumbMP, 2 },
                    { VNHumanHandPoseObservationJointName.ThumbIP, 3 },
                    { VNHumanHandPoseObservationJointName.ThumbTip, 4 },

                    // Index finger
                    { VNHumanHandPoseObservationJointName.IndexMcp, 5 },
                    { VNHumanHandPoseObservationJointName.IndexPip, 6 },
                    { VNHumanHandPoseObservationJointName.IndexDip, 7 },
                    { VNHumanHandPoseObservationJointName.IndexTip, 8 },

                    // Middle finger
                    { VNHumanHandPoseObservationJointName.MiddleMcp, 9 },
                    { VNHumanHandPoseObservationJointName.MiddlePip, 10 },
                    { VNHumanHandPoseObservationJointName.MiddleDip, 11 },
                    { VNHumanHandPoseObservationJointName.MiddleTip, 12 },

                    // Ring finger
                    { VNHumanHandPoseObservationJointName.RingMcp, 13 },
                    { VNHumanHandPoseObservationJointName.RingPip, 14 },
                    { VNHumanHandPoseObservationJointName.RingDip, 15 },
                    { VNHumanHandPoseObservationJointName.RingTip, 16 },

                    // Little finger
                    { VNHumanHandPoseObservationJointName.LittleMcp, 17 },
                    { VNHumanHandPoseObservationJointName.LittlePip, 18 },
                    { VNHumanHandPoseObservationJointName.LittleDip, 19 },
                    { VNHumanHandPoseObservationJointName.LittleTip, 20 }
                };

                foreach (var (jointName, index) in landmarkMap)
                {
                    NSError error;
                    var recognizedPoint = observation.GetRecognizedPoint(jointName, out error);

                    if (error == null && recognizedPoint != null)
                    {
                        // Vision coordinates: bottom-left origin, normalized
                        // Convert to top-left origin for consistency
                        var x = recognizedPoint.Location.X;
                        var y = 1.0f - recognizedPoint.Location.Y; // Flip Y axis

                        landmarks.Add(new HandLandmark
                        {
                            X = (float)x,
                            Y = (float)y,
                            Z = 0, // Vision doesn't provide Z depth for hands
                            Index = index,
                            Type = (HandLandmarkType)index
                        });
                    }
                }

                // Sort by index to ensure correct order
                landmarks = landmarks.OrderBy(l => l.Index).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Hand landmark conversion error: {ex.Message}");
            }

            return landmarks;
        }

        /// <summary>
        /// Determine if hand is left or right
        /// Vision provides chirality (handedness)
        /// </summary>
        private HandType DetermineHandedness(VNHumanHandPoseObservation observation)
        {
            try
            {
                // Vision provides chirality property
                switch (observation.Chirality)
                {
                    case VNChirality.Left:
                        return HandType.Left;

                    case VNChirality.Right:
                        return HandType.Right;

                    default:
                        return HandType.Unknown;
                }
            }
            catch
            {
                // Fallback: estimate from landmark positions
                return EstimateHandednessFromLandmarks(observation);
            }
        }

        /// <summary>
        /// Fallback method to estimate handedness from landmark positions
        /// </summary>
        private HandType EstimateHandednessFromLandmarks(VNHumanHandPoseObservation observation)
        {
            try
            {
                NSError error;

                // Get wrist and middle finger base positions
                var wrist = observation.GetRecognizedPoint(VNHumanHandPoseObservationJointName.Wrist, out error);
                var middleMcp = observation.GetRecognizedPoint(VNHumanHandPoseObservationJointName.MiddleMcp, out error);
                var thumbCmc = observation.GetRecognizedPoint(VNHumanHandPoseObservationJointName.ThumbCmc, out error);

                if (wrist != null && middleMcp != null && thumbCmc != null)
                {
                    // Calculate cross product to determine orientation
                    var dx1 = middleMcp.Location.X - wrist.Location.X;
                    var dy1 = middleMcp.Location.Y - wrist.Location.Y;

                    var dx2 = thumbCmc.Location.X - wrist.Location.X;
                    var dy2 = thumbCmc.Location.Y - wrist.Location.Y;

                    var cross = dx1 * dy2 - dy1 * dx2;

                    // Positive cross product suggests right hand, negative suggests left
                    return cross > 0 ? HandType.Right : HandType.Left;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Handedness estimation error: {ex.Message}");
            }

            return HandType.Unknown;
        }

        public override void Dispose()
        {
            _handPoseRequest?.Dispose();
            _requestHandler?.Dispose();
            _handPoseRequest = null;
            _requestHandler = null;
            base.Dispose();
        }
    }
}