namespace Mediapipe.Maui.Models
{
    /// <summary>
    /// Event arguments for drowsiness detection events
    /// </summary>
    public class DrowsinessDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Current Eye Aspect Ratio
        /// </summary>
        public float EyeAspectRatio { get; set; }

        /// <summary>
        /// Left eye aspect ratio
        /// </summary>
        public float LeftEyeAspectRatio { get; set; }

        /// <summary>
        /// Right eye aspect ratio
        /// </summary>
        public float RightEyeAspectRatio { get; set; }

        /// <summary>
        /// Number of consecutive drowsy frames
        /// </summary>
        public int ConsecutiveFrames { get; set; }

        /// <summary>
        /// Duration of drowsiness
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Severity level (0-1, higher is more severe)
        /// </summary>
        public float Severity { get; set; }

        /// <summary>
        /// Timestamp when drowsiness was detected
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Additional context or reason
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Whether the eyes are completely closed
        /// </summary>
        public bool EyesClosed { get; set; }

        public DrowsinessDetectedEventArgs()
        {
        }

        public DrowsinessDetectedEventArgs(float ear, int frames, TimeSpan duration)
        {
            EyeAspectRatio = ear;
            ConsecutiveFrames = frames;
            Duration = duration;

            // Calculate severity based on EAR and duration
            Severity = CalculateSeverity(ear, duration);
        }

        private float CalculateSeverity(float ear, TimeSpan duration)
        {
            // EAR contribution (0-0.5)
            float earScore = Math.Max(0, (0.25f - ear) / 0.25f) * 0.5f;

            // Duration contribution (0-0.5)
            float durationScore = Math.Min(0.5f, (float)duration.TotalSeconds / 10f * 0.5f);

            return Math.Min(1.0f, earScore + durationScore);
        }

        public override string ToString()
        {
            return $"Drowsiness: EAR={EyeAspectRatio:F3}, Duration={Duration.TotalSeconds:F1}s, Severity={Severity:F2}";
        }
    }
}