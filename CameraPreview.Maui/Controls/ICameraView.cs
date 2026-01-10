namespace CameraPreview.Maui.Controls
{
    /// <summary>
    /// Platform-agnostic camera view interface
    /// </summary>
    public interface ICameraView
    {
        /// <summary>
        /// Start camera preview
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stop camera preview
        /// </summary>
        void Stop();

        /// <summary>
        /// Takes a capture form the active camera playback.
        /// </summary>
        /// <param name="imageFormat">The capture image format</param>
        Task<ImageSource> GetSnapShotAsync(ImageFormat imageFormat = ImageFormat.Png);

        /// <summary>
        /// Saves a capture form the active camera playback in a file
        /// </summary>
        /// <param name="imageFormat">The capture image format</param>
        /// <param name="SnapFilePath">Full path for the file</param>
        Task<bool> SaveSnapShotAsync(ImageFormat imageFormat, string SnapFilePath);

        /// <summary>
        /// Get camera running status
        /// </summary>
        bool IsRunning { get; }
    }

    /// <summary>
    /// Event args for camera frame
    /// </summary>
    public class CameraFrameEventArgs : EventArgs
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public int Width { get; set; }
        public int Height { get; set; }
        public long Timestamp { get; set; }
        public int RotationDegrees { get; set; }
    }
}