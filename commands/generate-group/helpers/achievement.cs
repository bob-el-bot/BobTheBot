using System.Threading.Tasks;
using SkiaSharp;

namespace Bob.Commands.Helpers
{
    public static class AchievementImageGenerator
    {
        /// <summary>
        /// Generate an "achievement unlocked" style toast with an icon, title, and description.
        /// </summary>
        /// <param name="title">The title of the achievement, shown in yellow.</param>
        /// <param name="description">The achievement description.</param>
        /// <param name="iconUrl">The URL of the icon image.</param>
        /// <returns>A Task representing the asynchronous operation that returns the generated SKBitmap image. Returns null if the given iconUrl gets a null image.</returns>
        public static async Task<SKBitmap> GenerateAchievementImage(string title, string description, string iconUrl)
        {
            SKBitmap iconBitmap = await ImageCache.GetImageFromUrl(iconUrl);
            if (iconBitmap == null)
            {
                return null;
            }

            int scaleFactor = 2;
            int width = 640;
            int height = 128;
            var bitmap = new SKBitmap(width * scaleFactor, height * scaleFactor);
            using var canvas = new SKCanvas(bitmap);
            canvas.Scale(scaleFactor);
            canvas.Clear(SKColors.Transparent);

            // Toast background with a light border
            var toastRect = new SKRoundRect(new SKRect(4, 4, width - 4, height - 4), 10);
            using (var backgroundPaint = new SKPaint { Color = new SKColor(33, 33, 33), IsAntialias = true })
            {
                canvas.DrawRoundRect(toastRect, backgroundPaint);
            }
            using (var borderPaint = new SKPaint { Color = new SKColor(85, 85, 85), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4 })
            {
                canvas.DrawRoundRect(toastRect, borderPaint);
            }

            // Icon
            float iconSize = 80;
            float iconX = 24;
            float iconY = (height - iconSize) / 2;
            var iconRect = new SKRoundRect(new SKRect(iconX, iconY, iconX + iconSize, iconY + iconSize), 8);
            canvas.Save();
            canvas.ClipRoundRect(iconRect, antialias: true);
            GenerateImageUtils.DrawImageCover(canvas, iconBitmap, iconRect.Rect);
            canvas.Restore();

            // Title and description
            float textX = iconX + iconSize + 20;
            float textWidth = width - textX - 24;

            using var titlePaint = new SKPaint
            {
                Color = new SKColor(255, 221, 0),
                TextSize = 26,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };

            using var descriptionPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 22,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial")
            };

            string titleLine = GenerateImageUtils.TruncateText(title, textWidth, titlePaint);
            var descriptionLines = GenerateImageUtils.WrapText(description, textWidth, descriptionPaint, 2);

            float lineHeight = descriptionPaint.TextSize + 6;
            float blockHeight = titlePaint.TextSize + 10 + descriptionLines.Count * lineHeight;
            float y = (height - blockHeight) / 2 + titlePaint.TextSize;

            canvas.DrawText(titleLine, textX, y, titlePaint);
            y += 10 + lineHeight;
            foreach (var line in descriptionLines)
            {
                canvas.DrawText(line, textX, y, descriptionPaint);
                y += lineHeight;
            }

            return bitmap;
        }
    }
}
