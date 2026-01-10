using Mediapipe.Maui.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mediapipe.Maui.Controls
{
    public class FaceMeshDrawable : IDrawable
    {
        public FaceMeshResult Results { get; set; }

        public float ScaleFactor { get; set; } = 1f;
        public float ImageWidth { get; set; } = 1;
        public float ImageHeight { get; set; } = 1;

        private const float LANDMARK_STROKE_WIDTH = 1f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Results?.Landmarks == null || Results.Landmarks.Count == 0)
                return;

            canvas.StrokeSize = LANDMARK_STROKE_WIDTH;
            canvas.StrokeColor = Colors.Green;
            canvas.FillColor = Colors.Yellow;

            var scaledImageWidth = ImageWidth * ScaleFactor;
            var scaledImageHeight = ImageHeight * ScaleFactor;

            var offsetX = (dirtyRect.Width - scaledImageWidth) / 2f;
            var offsetY = (dirtyRect.Height - scaledImageHeight) / 2f;

            foreach (var face in Results.Landmarks)
            {
                DrawLandmarks(canvas, face, offsetX, offsetY);
                DrawConnectors(canvas, face, offsetX, offsetY);
            } 
        }

        private void DrawLandmarks(
            ICanvas canvas,
            List<FaceLandmark> landmarks,
            float offsetX,
            float offsetY)
        {
            foreach (var lm in landmarks)
            {
                float x = lm.X * ImageWidth * ScaleFactor;
                float y = lm.Y * ImageHeight * ScaleFactor;

                canvas.FillCircle(x + offsetX, y + offsetY, 2.5f);
            }
        }

        private void DrawConnectors(
            ICanvas canvas,
            List<FaceLandmark> landmarks,
            float offsetX,
            float offsetY)
        {
            foreach (var (start, end) in FaceMeshConnections.GetAllConnections())
            {
                if (start >= landmarks.Count || end >= landmarks.Count)
                    continue;

                var s = landmarks[start];
                var e = landmarks[end];

                float sx = s.X * ImageWidth * ScaleFactor;
                float sy = s.Y * ImageHeight * ScaleFactor;
                float ex = e.X * ImageWidth * ScaleFactor;
                float ey = e.Y * ImageHeight * ScaleFactor;

                canvas.DrawLine(
                    sx + offsetX, sy + offsetY,
                    ex + offsetX, ey + offsetY);
            }
        }
    }
}
