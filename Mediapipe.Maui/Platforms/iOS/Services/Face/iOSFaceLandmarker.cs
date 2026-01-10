using CoreGraphics;
using CoreImage;
using Foundation;
using Mediapipe.Maui.Models;
using Mediapipe.Maui.Services;
using System.Diagnostics;
using Vision;

namespace Mediapipe.Maui.Platforms.iOS.Services
{
    public class iOSFaceLandmarker : MediaPipeDetectorBase<FaceLandmarksResult>
    {
        private VNDetectFaceLandmarksRequest _landmarksRequest;
        private VNSequenceRequestHandler _requestHandler;

        public override string DetectorName => "iOS Face Landmarker (Vision)";

        protected override Task InitializeDetectorAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    // Create face landmarks detection request
                    _landmarksRequest = new VNDetectFaceLandmarksRequest(HandleFaceDetection)
                    {
                        // Use accurate mode for better quality
                        Revision = VNDetectFaceLandmarksRequestRevision.Three
                    };

                    _requestHandler = new VNSequenceRequestHandler();

                    Debug.WriteLine("iOSFaceLandmarker initialized with Vision Framework");
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
                    if (_landmarksRequest == null || _requestHandler == null)
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
                    _requestHandler.Perform(new[] { _landmarksRequest }, ciImage, out error);

                    if (error != null)
                    {
                        Debug.WriteLine($"Vision request error: {error.LocalizedDescription}");
                        return result;
                    }

                    // Get results
                    var observations = _landmarksRequest.GetResults<VNFaceObservation>();

                    if (observations != null && observations.Length > 0)
                    {
                        result.IsDetected = true;

                        // Process each detected face
                        foreach (var observation in observations.Take(_options.MaxNumResults))
                        {
                            var faceLandmarks = ConvertVisionLandmarksToMediaPipe(observation);
                            result.Faces.Add(faceLandmarks);
                            result.DetectionConfidence = observation.Confidence;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"iOS Face detection error: {ex.Message}");
                }

                return result;
            });
        }

        private void HandleFaceDetection(VNRequest request, NSError error)
        {
            // Callback - results are retrieved in PerformDetectionAsync
            if (error != null)
            {
                Debug.WriteLine($"Face detection callback error: {error.LocalizedDescription}");
            }
        }

        /// <summary>
        /// Convert Vision's 76 landmarks to MediaPipe-compatible format
        /// Vision landmarks are mapped to equivalent MediaPipe indices
        /// </summary>
        private List<FaceLandmark> ConvertVisionLandmarksToMediaPipe(VNFaceObservation observation)
        {
            var landmarks = new List<FaceLandmark>();

            if (observation.Landmarks == null)
                return landmarks;

            var imageBounds = observation.BoundingBox;
            var visionLandmarks = observation.Landmarks;

            // Map Vision landmarks to MediaPipe-style indices
            // Note: This is an approximation since Vision has 76 vs MediaPipe's 468

            // Face contour (Vision: faceContour)
            if (visionLandmarks.FaceContour != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.FaceContour, imageBounds, 0);
            }

            // Left eye (Vision: leftEye)
            if (visionLandmarks.LeftEye != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.LeftEye, imageBounds, 33);
            }

            // Right eye (Vision: rightEye)
            if (visionLandmarks.RightEye != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.RightEye, imageBounds, 263);
            }

            // Left eyebrow (Vision: leftEyebrow)
            if (visionLandmarks.LeftEyebrow != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.LeftEyebrow, imageBounds, 46);
            }

            // Right eyebrow (Vision: rightEyebrow)
            if (visionLandmarks.RightEyebrow != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.RightEyebrow, imageBounds, 276);
            }

            // Nose (Vision: nose, noseCrest)
            if (visionLandmarks.Nose != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.Nose, imageBounds, 1);
            }

            if (visionLandmarks.NoseCrest != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.NoseCrest, imageBounds, 6);
            }

            // Outer lips (Vision: outerLips)
            if (visionLandmarks.OuterLips != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.OuterLips, imageBounds, 61);
            }

            // Inner lips (Vision: innerLips)
            if (visionLandmarks.InnerLips != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.InnerLips, imageBounds, 78);
            }

            // Left pupil (Vision: leftPupil)
            if (visionLandmarks.LeftPupil != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.LeftPupil, imageBounds, 468);
            }

            // Right pupil (Vision: rightPupil)
            if (visionLandmarks.RightPupil != null)
            {
                AddLandmarksFromRegion(landmarks, visionLandmarks.RightPupil, imageBounds, 473);
            }

            // Sort by index to maintain order
            return landmarks.OrderBy(l => l.Index).ToList();
        }

        private void AddLandmarksFromRegion(
            List<FaceLandmark> landmarks,
            VNFaceLandmarkRegion2D region,
            CGRect bounds,
            int startIndex)
        {
            var points = region.NormalizedPoints;

            for (int i = 0; i < region.PointCount.ToUInt32(); i++)
            {
                var point = points[i];

                // Convert from Vision coordinates (bottom-left origin) to normalized (0-1)
                // Vision uses face-relative coordinates, we need image-relative
                var x = bounds.X + (point.X * bounds.Width);
                var y = 1.0f - (bounds.Y + (point.Y * bounds.Height)); // Flip Y

                landmarks.Add(new FaceLandmark
                {
                    X = (float)x,
                    Y = (float)y,
                    Z = 0, // Vision doesn't provide Z depth
                    Index = startIndex + i,
                    Visibility = 1.0f
                });
            }
        }

        public override void Dispose()
        {
            _landmarksRequest?.Dispose();
            _requestHandler?.Dispose();
            _landmarksRequest = null;
            _requestHandler = null;
            base.Dispose();
        }
    }
}