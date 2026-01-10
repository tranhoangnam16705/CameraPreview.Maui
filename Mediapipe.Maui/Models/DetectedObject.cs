using System.Drawing;

namespace Mediapipe.Maui.Models
{
    public class DetectedObject
    {
        public string Label { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public RectangleF BoundingBox { get; set; }
    }
}