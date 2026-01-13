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
        private MPPFaceLandmarker _landmarker;

        public override string DetectorName => "iOS Face Landmarker";

        protected override Task InitializeDetectorAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    var modelPath = await LandmarkModelProvider
                                .GetModelPathAsync("face_landmarker.task");

                    var baseOptions = new MPPBaseOptions
                    {
                        ModelAssetPath = modelPath
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
                    _landmarker = new MPPFaceLandmarker(options, out error);

                    if (error != null)
                    {
                        Debug.WriteLine($"iOSFaceLandmarker initialization error: {error.LocalizedDescription}");
                        throw new InvalidOperationException($"Failed to initialize face landmarker: {error.LocalizedDescription}");
                    }

                    Debug.WriteLine("iOSFaceLandmarker initialized successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"iOSFaceLandmarker init error: {ex.Message}");
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
                    if (_landmarker == null)
                        return result;

                    using var nsData = NSData.FromArray(imageData);
                    var uiImage = UIImage.LoadFromData(nsData);

                    if (uiImage == null)
                    {
                        Debug.WriteLine("Failed to create UIImage from byte array");
                        return result;
                    }

                    NSError imageError;
                    var mpImage = new MPPImage(uiImage, out imageError);

                    if (imageError != null)
                    {
                        Debug.WriteLine($"MPPImage creation failed: {imageError.LocalizedDescription}");
                        uiImage.Dispose();
                        return result;
                    }

                    NSError detectError;
                    var faceResult = _landmarker.DetectImage(mpImage, out detectError);

                    if (detectError != null)
                    {
                        Debug.WriteLine($"Face detection error: {detectError.LocalizedDescription}");
                        uiImage.Dispose();
                        return result;
                    }

                    if (faceResult?.FaceLandmarks != null && faceResult.FaceLandmarks.Length > 0)
                    {
                        result.IsDetected = true;
                        var faceCount = (int)faceResult.FaceLandmarks.Length;

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

                    uiImage.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"iOS Face detection error: {ex.Message}");
                }

                return result;
            });
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
            _landmarker = null;
            base.Dispose();
        }
    }
}