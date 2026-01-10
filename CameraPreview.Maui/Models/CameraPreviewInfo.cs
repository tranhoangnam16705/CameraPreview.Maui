using CameraPreview.Maui.Models;

namespace CameraPreview.Maui
{
    public class CameraPreviewInfo
    {
        public string Name { get; internal set; }
        public string DeviceId { get; internal set; }
        public CameraPreviewPosition Position { get; internal set; }
        public bool HasFlashUnit { get; internal set; }
        public float MinZoomFactor { get; internal set; }
        public float MaxZoomFactor { get; internal set; }
        public float HorizontalViewAngle { get; internal set; }
        public float VerticalViewAngle { get; internal set; }

        public List<Size> AvailableResolutions { get; internal set; }

        public override string ToString()
        {
            return Name;
        }
    }
}