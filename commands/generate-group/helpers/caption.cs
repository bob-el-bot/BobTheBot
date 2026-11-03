using System;
using System.Threading.Tasks;
using SkiaSharp;

namespace Bob.Commands.Helpers
{
    public static class CaptionImageGenerator
    {
        /// <summary>
        /// Generate a caption meme with bold text on a white band above the image.
        /// </summary>
        /// <param name="imageUrl">The URL of the image to caption.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="customBackgroundColor">A caption band color to use instead of white, or null.</param>
        /// <param name="customTextColor">A caption text color to use instead of black, or null.</param>
        /// <returns>A Task representing the asynchronous operation that returns the generated SKBitmap image. Returns null if the given imageUrl gets a null image.</returns>
        public static async Task<SKBitmap> GenerateCaptionImage(string imageUrl, string caption, SKColor? customBackgroundColor = null, SKColor? customTextColor = null)
        {
            SKBitmap sourceBitmap = await ImageCache.GetImageFromUrl(imageUrl);
            if (sourceBitmap == null)
            {
                return null;
            }

            // Normalize the width so the caption looks the same regardless of the source image size
            int width = 800;
            int imageHeight = (int)Math.Round((double)sourceBitmap.Height * width / sourceBitmap.Width);
            // Extreme aspect ratios get cropped instead of becoming a sliver or a giant strip
            imageHeight = Math.Clamp(imageHeight, 200, 1600);

            using var captionPaint = new SKPaint
            {
                Color = customTextColor ?? SKColors.Black,
                TextSize = 56,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Impact", SKFontStyle.Bold)
            };

            float padding = 36;
            float lineHeight = captionPaint.TextSize * 1.15f;
            var lines = GenerateImageUtils.WrapText(caption, width - padding * 2, captionPaint, 6);
            int bandHeight = (int)(lines.Count * lineHeight + padding * 2);

            var bitmap = new SKBitmap(width, bandHeight + imageHeight);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(customBackgroundColor ?? SKColors.White);

            // Center the text block within the band
            var metrics = captionPaint.FontMetrics;
            float y = padding + (lineHeight - (metrics.Descent - metrics.Ascent)) / 2 - metrics.Ascent;
            foreach (var line in lines)
            {
                canvas.DrawText(line, width / 2f, y, captionPaint);
                y += lineHeight;
            }

            GenerateImageUtils.DrawImageCover(canvas, sourceBitmap, new SKRect(0, bandHeight, width, bandHeight + imageHeight));

            return bitmap;
        }
    }
}
