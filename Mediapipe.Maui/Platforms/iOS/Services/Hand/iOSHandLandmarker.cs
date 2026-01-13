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
    /// iOS Hand Landmarker using MediaPipe Tasks Vision (MPPHandLandmarker)
    /// Detects 21 hand landmarks for hand tracking and gesture recognition
    /// </summary>
    public class iOSHandLandmarker : MediaPipeDetectorBase<HandLandmarksResult>
    {
        private MPPHandLandmarker _handLandmarker;

        public override string DetectorName => "iOS Hand Landmarker";

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
                        NumHands = _options.MaxNumResults,
                        MinHandDetectionConfidence = _options.MinDetectionConfidence,
                        MinHandPresenceConfidence = _options.MinDetectionConfidence,
                        MinTrackingConfidence = _options.MinTrackingConfidence
                    };

                    NSError error;
                    _handLandmarker = new MPPHandLandmarker(options, out error);

                    if (error != null)
                    {
                        Debug.WriteLine($"iOSHandLandmarker initialization error: {error.LocalizedDescription}");
                        throw new InvalidOperationException($"Failed to initialize hand landmarker: {error.LocalizedDescription}");
                    }

                    Debug.WriteLine("iOSHandLandmarker initialized successfully");
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

                if (_handLandmarker == null)
                    return result;

                UIImage uiImage = null;
                MPPImage mpImage = null;

                try
                {
                    using var nsData = NSData.FromArray(imageData);
                    uiImage = UIImage.LoadFromData(nsData);

                    if (uiImage == null)
                    {
                        Debug.WriteLine("Failed to create UIImage from byte array");
                        return result;
                    }

                    NSError imageError;
                    mpImage = new MPPImage(uiImage, out imageError);

                    if (imageError != null)
                    {
                        Debug.WriteLine($"MPPImage creation failed: {imageError.LocalizedDescription}");
                        return result;
                    }

                    NSError detectError;
                    var handResult = _handLandmarker.DetectImage(mpImage, out detectError);

                    if (detectError != null)
                    {
                        Debug.WriteLine($"Hand detection error: {detectError.LocalizedDescription}");
                        return result;
                    }

                    if (handResult?.Landmarks != null && handResult.Landmarks.Length > 0)
                    {
                        result.IsDetected = true;
                        var handCount = (int)handResult.Landmarks.Length;

                        // Convert all detected hands
                        for (int handIdx = 0; handIdx < handCount; handIdx++)
                        {
                            var landmarks = handResult.Landmarks[handIdx];
                            if (landmarks != null && landmarks.Count > 0)
                            {
                                var handLandmarks = ConvertHandLandmarks(landmarks);
                                if (handLandmarks.Count > 0)
                                {
                                    result.Hands.Add(handLandmarks);
                                }
                            }
                        }

                        // Get handedness (left/right)
                        // Handedness is NSArray<NSArray<MPPCategory>> - outer array per hand, inner array of categories
                        if (handResult.Handedness != null && handResult.Handedness.Length > 0)
                        {
                            var handednessCount = (int)handResult.Handedness.Length;
                            for (int i = 0; i < handednessCount; i++)
                            {
                                var handednessCategories = handResult.Handedness[i];
                                if (handednessCategories != null && handednessCategories.Count > 0)
                                {
                                    var category = handednessCategories[0];
                                    if (category != null)
                                    {
                                        var label = category.CategoryName;
                                        result.Handedness.Add(label == "Left" ? HandType.Left : HandType.Right);
                                    }
                                    else
                                    {
                                        result.Handedness.Add(HandType.Unknown);
                                    }
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
                finally
                {
                    // Always dispose resources in reverse order of creation
                    mpImage?.Dispose();
                    uiImage?.Dispose();
                }

                return result;
            });
        }

        private List<Models.HandLandmark> ConvertHandLandmarks(NSArray<MPPNormalizedLandmark> landmarks)
        {
            var result = new List<Models.HandLandmark>((int)landmarks.Count);
            var count = (int)landmarks.Count;

            for (int i = 0; i < count; i++)
            {
                var landmark = landmarks[i];
                if (landmark != null)
                {
                    result.Add(new Models.HandLandmark
                    {
                        X = landmark.X,
                        Y = landmark.Y,
                        Z = landmark.Z,
                        Index = i,
                        Type = (HandLandmarkType)i
                    });
                }
            }

            return result;
        }

        public override void Dispose()
        {
            _handLandmarker = null;
            base.Dispose();
        }
    }
}