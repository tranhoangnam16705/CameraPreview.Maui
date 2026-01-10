using Foundation;
using Mediapipe.Maui.LandmarkModels;
using Mediapipe.Maui.Models;
using Mediapipe.Maui.Services;
using MediaPipeTasksVision;
using System.Diagnostics;
using UIKit;

namespace Mediapipe.Maui.Platforms.iOS.Services
{
    /// <summary>
    /// iOS Hand Landmarker using Apple Vision Framework
    /// Vision provides 21 hand landmarks (same as MediaPipe)
    /// </summary>
    public class iOSHandLandmarker : MediaPipeDetectorBase<HandLandmarksResult>
    {
        private MPPHandLandmarker _handLandmarker;

        public override string DetectorName => "iOS Hand Landmarker (Vision)";

        protected override Task InitializeDetectorAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    var modelPath = await LandmarkModelProvider
                                .GetModelPathAsync("hand_landmarker.task");

                    var baseOptions = new MPPBaseOptions
                    {
                        ModelAssetPath = modelPath
                    };

                    var options = new MPPHandLandmarkerOptions
                    {
                        BaseOptions = baseOptions,
                        RunningMode = MPPRunningMode.Image,
                        NumHands = _options.MaxNumResults
                    };

                    NSError error;
                    _handLandmarker = new MPPHandLandmarker(options, out error);
                    if (error != null)
                        Debug.WriteLine("iOSFaceLandmarker initialized with Vision Framework :" + error);
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
                    using var nsData = NSData.FromArray(imageData);
                    using var uiImage = UIImage.LoadFromData(nsData);

                    NSError error;
                    // Create CIImage for Vision
                    using var mpImage = new MPPImage(uiImage, out error);

                    var handResult = _handLandmarker.DetectImage(mpImage, out error);
                    var countHand = handResult?.Landmarks.Count();
                    if (countHand > 0)
                    {
                        result.IsDetected = true;

                        // Convert all detected hands
                        for (int handIdx = 0; handIdx < countHand; handIdx++)
                        {
                            var landmarks = handResult.Landmarks[handIdx];
                            var handLandmarks = new List<Models.HandLandmark>();

                            // Convert MediaPipe landmarks to our model
                            handLandmarks = ConvertHandLandmarks(landmarks);
                            if (handLandmarks != null && handLandmarks.Count > 0)
                            {
                                result.Hands.Add(handLandmarks);
                            }
                        }
                        var handednessCount = handResult.Handedness.Count();
                        // Get handedness (left/right)
                        if (handednessCount > 0)
                        {
                            for (int i = 0; i < handednessCount; i++)
                            {
                                var handedness = handResult.Handedness[i];
                                var category = handedness.Count > 0 ? handedness[0] : null;

                                if (category != null)
                                {
                                    var label = (category as MediaPipeTasksVision.MPPCategory)?.CategoryName;
                                    result.Handedness.Add(label == "Left" ? HandType.Left : HandType.Right);
                                }
                                else
                                {
                                    result.Handedness.Add(HandType.Unknown);
                                }
                            }
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

        private List<Models.HandLandmark> ConvertHandLandmarks(NSArray<MPPNormalizedLandmark> landmarks)
        {
            var handLandmarks = new List<Models.HandLandmark>();
            for (int i = 0; i < landmarks.Count.ToUInt32(); i++)
            {
                var landmark = landmarks[i];
                handLandmarks.Add(new Models.HandLandmark
                {
                    X = landmark.X,
                    Y = landmark.Y,
                    Z = landmark.Z,
                    Index = i,
                    Type = (HandLandmarkType)i
                });
            }

            return handLandmarks;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}