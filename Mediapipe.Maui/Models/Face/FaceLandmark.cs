namespace Mediapipe.Maui.Models
{
    /// <summary>
    /// Represents a single facial landmark point in 3D space
    /// </summary>
    public class FaceLandmark
    {
        /// <summary>
        /// X coordinate (normalized 0-1, relative to image width)
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y coordinate (normalized 0-1, relative to image height)
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Z coordinate (depth, relative to face center)
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// Landmark index (0-477 for full face mesh)
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Optional: visibility/presence confidence (0-1)
        /// </summary>
        public float Visibility { get; set; } = 1.0f;

        public FaceLandmark()
        {
        }

        public FaceLandmark(float x, float y, float z, int index = 0)
        {
            X = x;
            Y = y;
            Z = z;
            Index = index;
        }

        public override string ToString()
        {
            return $"Landmark[{Index}]: ({X:F3}, {Y:F3}, {Z:F3})";
        }

        /// <summary>
        /// Calculate Euclidean distance to another landmark
        /// </summary>
        public float DistanceTo(FaceLandmark other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            float dz = Z - other.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Calculate 2D distance (ignoring Z)
        /// </summary>
        public float Distance2DTo(FaceLandmark other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public enum FaceRunningMode
    {
        Image,
        Video,
        LiveStream
    }
}