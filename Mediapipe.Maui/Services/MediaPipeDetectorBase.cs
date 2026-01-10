using Mediapipe.Maui.Interfaces;
using Mediapipe.Maui.Models;

namespace Mediapipe.Maui.Services
{
    /// <summary>
    /// Abstract base class for MediaPipe detectors
    /// </summary>
    public abstract class MediaPipeDetectorBase<TResult> : IMediaPipeDetector<TResult>
        where TResult : IDetectionResult, new()
    {
        protected MediaPipeOptions _options;
        protected bool _isInitialized;
        protected System.Diagnostics.Stopwatch _performanceTimer = new();

        public bool IsInitialized => _isInitialized;
        public abstract string DetectorName { get; }

        public virtual async Task InitializeAsync(MediaPipeOptions options)
        {
            _options = options;

            await InitializeDetectorAsync();

            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine($"{DetectorName} initialized");
        }

        public async Task<TResult> DetectAsync(byte[] imageData)
        {
            if (!_isInitialized)
                throw new InvalidOperationException($"{DetectorName} not initialized");

            _performanceTimer.Restart();

            var result = await PerformDetectionAsync(imageData);

            _performanceTimer.Stop();
            result.ProcessingTimeMs = _performanceTimer.Elapsed.TotalMilliseconds;
            result.Timestamp = DateTime.Now;

            return result;
        }

        async Task<IDetectionResult> IMediaPipeDetector.DetectAsync(byte[] imageData)
        {
            return await DetectAsync(imageData);
        }

        /// <summary>
        /// Platform-specific initialization
        /// </summary>
        protected abstract Task InitializeDetectorAsync();

        /// <summary>
        /// Platform-specific detection
        /// </summary>
        protected abstract Task<TResult> PerformDetectionAsync(byte[] imageData);

        public virtual void Dispose()
        {
            _isInitialized = false;
            System.Diagnostics.Debug.WriteLine($"{DetectorName} disposed");
        }
    }
}