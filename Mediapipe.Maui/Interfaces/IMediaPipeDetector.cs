using Mediapipe.Maui.Models;

namespace Mediapipe.Maui.Interfaces
{
    /// <summary>
    /// Generic MediaPipe detector interface
    /// </summary>
    public interface IMediaPipeDetector<TResult> : IMediaPipeDetector
        where TResult : IDetectionResult
    {
        /// <summary>
        /// Detect with typed result
        /// </summary>
        new Task<TResult> DetectAsync(byte[] imageData);
    }

    /// <summary>
    /// Base interface for all MediaPipe detectors
    /// </summary>
    public interface IMediaPipeDetector : IDisposable
    {
        /// <summary>
        /// Initialize the detector with model file
        /// </summary>
        Task InitializeAsync(MediaPipeOptions options);

        /// <summary>
        /// Detect from image data
        /// </summary>
        Task<IDetectionResult> DetectAsync(byte[] imageData);

        /// <summary>
        /// Check if detector is initialized
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Get detector name
        /// </summary>
        string DetectorName { get; }
    }
}