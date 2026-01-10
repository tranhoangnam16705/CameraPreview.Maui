using Android.Content;
using Android.Graphics;
using Mediapipe.Maui.LandmarkModels;
using Mediapipe.Maui.Models;
using Mediapipe.Maui.Services;
using MediaPipe.Framework.Image;
using MediaPipe.Tasks.Components.Containers;
using MediaPipe.Tasks.Core;
using MediaPipe.Tasks.Vision.Core;
using MediaPipe.Tasks.Vision.HandLandmarker;
using System.Diagnostics;

namespace Mediapipe.Maui.Platforms.Android.Services
{
    /// <summary>
    /// Android implementation of Hand Landmarker
    /// </summary>
    public class AndroidHandLandmarker : MediaPipeDetectorBase<HandLandmarksResult>
    {
        private readonly Context _context;
        private HandLandmarker _handLandmarker;

        public override string DetectorName => "Hand Landmarker";

        public AndroidHandLandmarker(Context context)
        {
            _context = context;
        }

        protected override Task InitializeDetectorAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    var modelPath = await LandmarkModelProvider
                                 .GetModelPathAsync("hand_landmarker.task");
                    var handBaseOptions = BaseOptions.InvokeBuilder()
                    .SetModelAssetPath(modelPath)
                    .Build();

                    var handOptions = HandLandmarker.HandLandmarkerOptions.InvokeBuilder()
                       .SetBaseOptions(handBaseOptions)
                       .SetNumHands((Java.Lang.Integer)_options.MaxNumResults)
                       .SetMinHandDetectionConfidence((Java.Lang.Float)_options.MinDetectionConfidence)
                       .SetMinHandPresenceConfidence((Java.Lang.Float)_options.MinDetectionConfidence)
                       .SetMinTrackingConfidence((Java.Lang.Float)_options.MinTrackingConfidence)
                       .SetRunningMode(RunningMode.Image)
                       .Build();

                    _handLandmarker = HandLandmarker.CreateFromOptions(_context, handOptions);
                    Debug.WriteLine("AndroidHandLandmarker created");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AndroidHandLandmarker init error: {ex.Message}");
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
                    if (_handLandmarker == null)
                        return result;

                    // Convert to bitmap
                    var bitmap = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);
                    if (bitmap == null) return result;

                    // Convert to MediaPipe image
                    var mpImage = new BitmapImageBuilder(bitmap).Build();

                    // Detect
                    var handResult = _handLandmarker.Detect(mpImage);

                    if (handResult?.Landmarks()?.Count > 0)
                    {
                        result.IsDetected = true;

                        // Convert all detected hands
                        for (int handIdx = 0; handIdx < handResult.Landmarks().Count; handIdx++)
                        {
                            var landmarks = handResult.Landmarks()[handIdx];
                            var handLandmarks = new List<Models.HandLandmark>();

                            // Convert MediaPipe landmarks to our model
                            handLandmarks = ConvertHandLandmarks(landmarks);
                            if (handLandmarks != null && handLandmarks.Count > 0)
                            {
                                result.Hands.Add(handLandmarks);
                            }
                        }

                        // Get handedness (left/right)
                        if (handResult.Handedness()?.Count > 0)
                        {
                            for (int i = 0; i < handResult.Handedness().Count; i++)
                            {
                                var handedness = handResult.Handedness()[i];
                                var category = handedness.Count > 0 ? handedness[0] : null;

                                if (category != null)
                                {
                                    var label = (category as Category)?.CategoryName();
                                    result.Handedness.Add(label == "Left" ? HandType.Left : HandType.Right);
                                }
                                else
                                {
                                    result.Handedness.Add(HandType.Unknown);
                                }
                            }
                        }
                    }

                    bitmap.Recycle();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Hand detection error: {ex.Message}");
                }

                return result;
            });
        }

        private List<Models.HandLandmark> ConvertHandLandmarks(IList<NormalizedLandmark> landmarks)
        {
            var handLandmarks = new List<Models.HandLandmark>();
            for (int i = 0; i < landmarks.Count; i++)
            {
                var landmark = (NormalizedLandmark)landmarks[i];
                handLandmarks.Add(new Models.HandLandmark
                {
                    X = landmark.X(),
                    Y = landmark.Y(),
                    Z = landmark.Z(),
                    Index = i,
                    Type = (HandLandmarkType)i
                });
            }

            return handLandmarks;
        }

        public override void Dispose()
        {
            _handLandmarker?.Close();
            _handLandmarker = null;
            base.Dispose();
        }
    }
}