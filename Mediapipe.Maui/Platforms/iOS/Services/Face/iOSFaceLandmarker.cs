using CoreGraphics;
using CoreImage;
using Foundation;
using Mediapipe.Maui.LandmarkModels;
using Mediapipe.Maui.Models;
using Mediapipe.Maui.Services;
using MediaPipeTasksVision;
using System.Diagnostics;
using UIKit;
using Vision;

namespace Mediapipe.Maui.Platforms.iOS.Services
{
    public class iOSFaceLandmarker : MediaPipeDetectorBase<FaceLandmarksResult>
    {
        private MPPFaceLandmarker _landmarker;
        public override string DetectorName => "iOS Face Landmarker (Vision)";

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
                        OutputFaceBlendshapes = _options.EnableFaceBlendshapes
                    };

                    NSError error;
                    _landmarker = new MPPFaceLandmarker(options, out error);
                    if (error != null)
                        Debug.WriteLine("iOSFaceLandmarker initialized with Vision Framework :" + error);
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
                    using var uiImage = UIImage.LoadFromData(nsData);

                    NSError mpError = null;
                    var mpImage = new MPPImage(uiImage,out mpError);
                    if (mpError != null)
                    {
                        Debug.WriteLine($"MPPImage creation failed: {mpError.LocalizedDescription}");
                        return result;
                    }
                    NSError error = null;
                    var faceResult = _landmarker.DetectImage(mpImage, out error);
                    var countFace = faceResult?.FaceLandmarks.Length;
                    if (countFace > 0)
                    {
                        result.IsDetected = true;

                        // Convert all detected faces
                        for (int faceIdx = 0; faceIdx < countFace; faceIdx++)
                        {
                            var face = faceResult?.FaceLandmarks;
                            if (face != null && face.Length > 0)
                            {
                                var landmarks = face[faceIdx];
                                if (landmarks != null)
                                {
                                    var faceLandmarks = new List<FaceLandmark>();

                                    // Convert MediaPipe landmarks to our model
                                    faceLandmarks = ConvertLandmarks(landmarks);
                                    if (faceLandmarks != null && faceLandmarks.Count > 0)
                                    {
                                        result.Faces.Add(faceLandmarks);
                                    }
                                }

                            }
                        }

                        // Get detection confidence (if available)
                        result.DetectionConfidence = 1.0f;
                    }
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
            var result = new List<FaceLandmark>();

            for (int i = 0; i < landmarks.Count.ToUInt32(); i++)
            {
                var landmark = landmarks[i];
                result.Add(new FaceLandmark
                {
                    X = landmark.X,
                    Y = landmark.Y,
                    Z = landmark.Z,
                    Index = i,
                    Visibility = landmark.Visibility != null ? landmark.Visibility.FloatValue : 1.0f
                });
            }

            return result;
        }

        public override void Dispose()
        {
            _landmarker = null;
            base.Dispose();
        }
    }
}