using Android.Content;
using Android.Graphics;
using Mediapipe.Maui.LandmarkModels;
using Mediapipe.Maui.Models;
using Mediapipe.Maui.Services;
using MediaPipe.Framework.Image;
using MediaPipe.Tasks.Components.Containers;
using MediaPipe.Tasks.Core;
using MediaPipe.Tasks.Vision.Core;
using MediaPipe.Tasks.Vision.FaceLandmarker;
using System.Diagnostics;

namespace Mediapipe.Maui.Platforms.Android.Services
{
    /// <summary>
    /// Android implementation of Face Landmarker
    /// </summary>
    public class AndroidFaceLandmarker : MediaPipeDetectorBase<FaceLandmarksResult>
    {
        private readonly Context _context;
        private FaceLandmarker _faceLandmarker;

        public override string DetectorName => "Face Landmarker";

        public AndroidFaceLandmarker(Context context)
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
                                 .GetModelPathAsync("face_landmarker.task");
                    var faceBaseOptions = BaseOptions.InvokeBuilder()
                    .SetModelAssetPath(modelPath)
                    .Build();

                    var faceOptions = FaceLandmarker.FaceLandmarkerOptions.InvokeBuilder()
                        .SetBaseOptions(faceBaseOptions)
                        .SetNumFaces((Java.Lang.Integer)_options.MaxNumResults)
                        .SetMinFaceDetectionConfidence((Java.Lang.Float)_options.MinDetectionConfidence)
                        .SetMinFacePresenceConfidence((Java.Lang.Float)_options.MinDetectionConfidence)
                        .SetMinTrackingConfidence((Java.Lang.Float)_options.MinTrackingConfidence)
                        .SetRunningMode(RunningMode.Image) // Use IMAGE for frame-by-frame
                        .SetOutputFaceBlendshapes(_options.EnableFaceBlendshapes)
                        .SetOutputFacialTransformationMatrixes(_options.EnableFacialTransformationMatrix)
                        .Build();

                    _faceLandmarker = FaceLandmarker.CreateFromOptions(_context, faceOptions);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AndroidFaceLandmarker init error: {ex.Message}");
                    throw;
                }
            });
        }

        protected override Task<FaceLandmarksResult> PerformDetectionAsync(byte[] imageData)
        {
            return Task.Run(() =>
            {
                var result = new FaceLandmarksResult();

                try
                {
                    if (_faceLandmarker == null)
                        return result;

                    // Convert to bitmap
                    var bitmap = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);
                    if (bitmap == null) return result;

                    // Convert to MediaPipe image
                    var mpImage = new BitmapImageBuilder(bitmap).Build();

                    // Detect
                    var faceResult = _faceLandmarker.Detect(mpImage);

                    if (faceResult?.FaceLandmarks()?.Count > 0)
                    {
                        result.IsDetected = true;

                        // Convert all detected faces
                        for (int faceIdx = 0; faceIdx < faceResult.FaceLandmarks().Count; faceIdx++)
                        {
                            var landmarks = faceResult.FaceLandmarks()[faceIdx];
                            var faceLandmarks = new List<FaceLandmark>();

                            // Convert MediaPipe landmarks to our model
                            faceLandmarks = ConvertLandmarks(landmarks);
                            if (faceLandmarks != null && faceLandmarks.Count > 0)
                            {
                                result.Faces.Add(faceLandmarks);
                            }
                        }

                        // Get detection confidence (if available)
                        result.DetectionConfidence = 1.0f;
                    }

                    bitmap.Recycle();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Face detection error: {ex.Message}");
                }

                return result;
            });
        }

        private List<FaceLandmark> ConvertLandmarks(IList<NormalizedLandmark> landmarks)
        {
            var result = new List<FaceLandmark>();

            for (int i = 0; i < landmarks.Count; i++)
            {
                var landmark = landmarks[i];
                float visibility = 1.0f;
                var opt = landmark.Visibility();
                if (opt != null && opt.IsPresent)
                {
                    visibility = ((Java.Lang.Float)opt.Get()).FloatValue();
                }
                result.Add(new FaceLandmark
                {
                    X = landmark.X(),
                    Y = landmark.Y(),
                    Z = landmark.Z(),
                    Index = i,
                    Visibility = visibility
                });
            }

            return result;
        }

        public override void Dispose()
        {
            _faceLandmarker?.Close();
            _faceLandmarker = null;
            base.Dispose();
        }
    }
}