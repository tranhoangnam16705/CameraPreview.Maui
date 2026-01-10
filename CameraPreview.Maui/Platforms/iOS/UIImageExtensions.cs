using CoreGraphics;
using UIKit;

namespace CameraPreview.Maui.Platforms.iOS
{
    public static class UIImageExtensions
    {
        /// <summary>
        /// Flip ảnh theo chiều ngang (Mirror horizontally)
        /// </summary>
        public static UIImage FlipImage(UIImage image)
        {
            try
            {
                var format = UIGraphicsImageRendererFormat.DefaultFormat;
                format.Scale = image.CurrentScale;
                format.Opaque = false;

                var renderer = new UIGraphicsImageRenderer(image.Size, format);

                return renderer.CreateImage(ctx =>
                {
                    var cgContext = ctx.CGContext;

                    // Translate to the right edge
                    cgContext.TranslateCTM(image.Size.Width, 0);

                    // Scale by -1 on X axis (flip horizontally)
                    cgContext.ScaleCTM(-1, 1);

                    // Draw the image
                    image.Draw(new CGRect(0, 0, image.Size.Width, image.Size.Height));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Flip image error: {ex.Message}");
                return image;
            }
        }
    }
}