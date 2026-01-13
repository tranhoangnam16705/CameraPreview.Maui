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
    /// iOS Face Landmarker using MediaPipe Tasks Vision (MPPFaceLandmarker)
    /// Detects 478 face landmarks for face mesh analysis
    /// </summary>
    public class iOSFaceLandmarker : MediaPipeDetectorBase<FaceLandmarksResult>
    {
        private volatile bool _isDetecting;
        private string _modelPath;

        public override string DetectorName => "iOS Face Landmarker";

        protected override Task InitializeDetectorAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    _modelPath = await LandmarkModelProvider
                                .GetModelPathAsync("face_landmarker.task");

                    Debug.WriteLine("iOSFaceLandmarker initialized successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"iOSFaceLandmarker init error: {ex.Message}");
                    throw;
                }
            });
        }

        private MPPFaceLandmarker CreateLandmarker()
        {
            var baseOptions = new MPPBaseOptions
            {
                ModelAssetPath = _modelPath
            };

            var options = new MPPFaceLandmarkerOptions
            {
                BaseOptions = baseOptions,
                RunningMode = MPPRunningMode.Image,
                NumFaces = _options.MaxNumResults,
                MinFaceDetectionConfidence = _options.MinDetectionConfidence,
                MinFacePresenceConfidence = _options.MinDetectionConfidence,
                MinTrackingConfidence = _options.MinTrackingConfidence,
                OutputFaceBlendshapes = _options.EnableFaceBlendshapes,
                OutputFacialTransformationMatrixes = _options.EnableFacialTransformationMatrix
            };

            NSError error;
            var landmarker = new MPPFaceLandmarker(options, out error);

            if (error != null)
            {
                Debug.WriteLine($"CreateLandmarker error: {error.LocalizedDescription}");
                return null;
            }

            return landmarker;
        }

        protected override Task<FaceLandmarksResult> PerformDetectionAsync(byte[] imageData)
        {
            var result = new FaceLandmarksResult();

            if (string.IsNullOrEmpty(_modelPath))
                return Task.FromResult(result);

            // Skip if another detection is in progress - simple flag check
            if (_isDetecting)
            {
                return Task.FromResult(result);
            }

            _isDetecting = true;

            try
            {
                // Run detection with fresh landmarker each time to avoid state issues
                result = DetectFaces(imageData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"iOS Face detection error: {ex.Message}");
            }
            finally
            {
                _isDetecting = false;
            }

            return Task.FromResult(result);
        }

        private FaceLandmarksResult DetectFaces(byte[] imageData)
        {
            var result = new FaceLandmarksResult();

            UIImage uiImage = null;
            MPPImage mpImage = null;
            MPPFaceLandmarker landmarker = null;

            try
            {
                // Create fresh landmarker for each detection to avoid state corruption
                landmarker = CreateLandmarker();
                if (landmarker == null)
                    return result;

                using var nsData = NSData.FromArray(imageData);
                uiImage = UIImage.LoadFromData(nsData);

                if (uiImage == null)
                    return result;

                NSError imageError;
                mpImage = new MPPImage(uiImage, out imageError);

                if (imageError != null)
                    return result;

                NSError detectError;
                var faceResult = landmarker.DetectImage(mpImage, out detectError);

                if (detectError != null)
                    return result;

                var faceCount = (int)(faceResult?.FaceLandmarks?.Length ?? 0);

                if (faceCount > 0)
                {
                    result.IsDetected = true;

                    for (int faceIdx = 0; faceIdx < faceCount; faceIdx++)
                    {
                        var landmarks = faceResult.FaceLandmarks[faceIdx];
                        if (landmarks != null && landmarks.Count > 0)
                        {
                            var faceLandmarks = ConvertLandmarks(landmarks);
                            if (faceLandmarks.Count > 0)
                            {
                                result.Faces.Add(faceLandmarks);
                            }
                        }
                    }

                    // Extract detection confidence from face blendshapes if available
                    result.DetectionConfidence = ExtractConfidence(faceResult);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"iOS Face detection error: {ex.Message}");
            }
            finally
            {
                // Always dispose resources in reverse order of creation
                mpImage?.Dispose();
                uiImage?.Dispose();
                landmarker?.Dispose();
            }

            return result;
        }

        private List<FaceLandmark> ConvertLandmarks(NSArray<MPPNormalizedLandmark> landmarks)
        {
            var result = new List<FaceLandmark>((int)landmarks.Count);
            var count = (int)landmarks.Count;

            for (int i = 0; i < count; i++)
            {
                var landmark = landmarks[i];
                if (landmark != null)
                {
                    result.Add(new FaceLandmark
                    {
                        X = landmark.X,
                        Y = landmark.Y,
                        Z = landmark.Z,
                        Index = i,
                        Visibility = landmark.Visibility?.FloatValue ?? 1.0f
                    });
                }
            }

            return result;
        }

        private float ExtractConfidence(MPPFaceLandmarkerResult faceResult)
        {
            // Try to get confidence from blendshapes if available
            if (faceResult.FaceBlendshapes != null && faceResult.FaceBlendshapes.Length > 0)
            {
                var blendshapes = faceResult.FaceBlendshapes[0];
                // MPPClassifications contains Categories array
                if (blendshapes?.Categories != null && blendshapes.Categories.Length > 0)
                {
                    // Use the first category's score as a confidence indicator
                    var category = blendshapes.Categories[0];
                    if (category != null)
                    {
                        return category.Score;
                    }
                }
            }

            // Default confidence when blendshapes not available
            return 1.0f;
        }

        public override void Dispose()
        {
            _modelPath = null;
            base.Dispose();
        }
    }
}