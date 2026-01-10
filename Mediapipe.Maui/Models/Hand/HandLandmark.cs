namespace Mediapipe.Maui.Models
{
    /// <summary>
    /// Represents a hand landmark point
    /// MediaPipe Hand has 21 landmarks per hand
    /// </summary>
    public class HandLandmark
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int Index { get; set; }

        /// <summary>
        /// Hand landmark names (0-20)
        /// 0: WRIST
        /// 1-4: THUMB (CMC, MCP, IP, TIP)
        /// 5-8: INDEX_FINGER (MCP, PIP, DIP, TIP)
        /// 9-12: MIDDLE_FINGER (MCP, PIP, DIP, TIP)
        /// 13-16: RING_FINGER (MCP, PIP, DIP, TIP)
        /// 17-20: PINKY (MCP, PIP, DIP, TIP)
        /// </summary>
        public HandLandmarkType Type { get; set; }

        public HandLandmark()
        {
        }

        public HandLandmark(float x, float y, float z, int index)
        {
            X = x;
            Y = y;
            Z = z;
            Index = index;
            Type = (HandLandmarkType)index;
        }

        public float DistanceTo(HandLandmark other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            float dz = Z - other.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    public enum HandLandmarkType
    {
        WRIST = 0,
        THUMB_CMC = 1,
        THUMB_MCP = 2,
        THUMB_IP = 3,
        THUMB_TIP = 4,
        INDEX_FINGER_MCP = 5,
        INDEX_FINGER_PIP = 6,
        INDEX_FINGER_DIP = 7,
        INDEX_FINGER_TIP = 8,
        MIDDLE_FINGER_MCP = 9,
        MIDDLE_FINGER_PIP = 10,
        MIDDLE_FINGER_DIP = 11,
        MIDDLE_FINGER_TIP = 12,
        RING_FINGER_MCP = 13,
        RING_FINGER_PIP = 14,
        RING_FINGER_DIP = 15,
        RING_FINGER_TIP = 16,
        PINKY_MCP = 17,
        PINKY_PIP = 18,
        PINKY_DIP = 19,
        PINKY_TIP = 20
    }

    public enum HandType
    {
        Unknown,
        Left,
        Right
    }
}