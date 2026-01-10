using Android.Graphics;

namespace CameraPreview.Maui.Platforms.Android
{
    public static class BitmapExtensions
    {
        public static MemoryStream BitmapToStream(Bitmap bitmap)
        {
            var stream = new MemoryStream();
            bitmap.Compress(Bitmap.CompressFormat.Jpeg, 95, stream);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            return stream;
        }
    }
}