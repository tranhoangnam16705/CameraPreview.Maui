namespace Mediapipe.Maui.Models
{
    /// <summary>
    /// Event arguments for smoking detection events
    /// </summary>
    public class SmokingDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Confidence score (0-1)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Distance between hand and mouth (normalized)
        /// </summary>
        public float HandToMouthDistance { get; set; }

        /// <summary>
        /// Which hand is detected near mouth
        /// </summary>
        public HandType DetectedHand { get; set; }

        /// <summary>
        /// Number of consecutive frames with smoking gesture
        /// </summary>
        public int ConsecutiveFrames { get; set; }

        /// <summary>
        /// Timestamp of detection
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Duration of the smoking gesture
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Specific gesture type detected
        /// </summary>
        public SmokingGestureType GestureType { get; set; }

        /// <summary>
        /// Additional context
        /// </summary>
        public string AdditionalInfo { get; set; }

        public override string ToString()
        {
            return $"Smoking: {GestureType}, Hand={DetectedHand}, Confidence={Confidence:F2}, Distance={HandToMouthDistance:F3}";
        }
    }

    public enum SmokingGestureType
    {
        Unknown,
        HandNearMouth,          // Hand close to mouth
        FingersTogether,        // Index and middle finger together (holding cigarette)
        HandToMouthMovement,    // Repeated hand-to-mouth movement
        Confirmed               // High confidence smoking detected
    }
}