using System;
using System.Globalization;
using System.Threading.Tasks;
using SkiaSharp;

namespace Bob.Commands.Helpers
{
    public static class TweetImageGenerator
    {
        /// <summary>
        /// The theme of the tweet image (light or dark).
        /// </summary>
        public enum Theme
        {
            Light,
            Dark
        }

        /// <summary>
        /// Generate a tweet image with the specified details.
        /// </summary>
        /// <param name="displayName">The display name of the poster.</param>
        /// <param name="handle">The handle of the poster, without the @.</param>
        /// <param name="avatarUrl">The URL of the poster's avatar image.</param>
        /// <param name="text">The tweet text.</param>
        /// <param name="likes">The number of likes on the tweet.</param>
        /// <param name="reposts">The number of reposts on the tweet.</param>
        /// <param name="verified">Whether to show a verified badge next to the display name.</param>
        /// <param name="theme">The theme of the tweet image (light or dark).</param>
        /// <returns>A Task representing the asynchronous operation that returns the generated SKBitmap image. Returns null if the given avatarUrl gets a null image.</returns>
        public static async Task<SKBitmap> GenerateTweetImage(string displayName, string handle, string avatarUrl, string text, int likes, int reposts, bool verified, Theme theme)
        {
            SKBitmap avatarBitmap = await ImageCache.GetImageFromUrl(avatarUrl);
            if (avatarBitmap == null)
            {
                return null;
            }

            bool isDarkTheme = theme == Theme.Dark;
            SKColor backgroundColor = isDarkTheme ? SKColors.Black : SKColors.White;
            SKColor textColor = isDarkTheme ? new SKColor(231, 233, 234) : new SKColor(15, 20, 25);
            SKColor subTextColor = isDarkTheme ? new SKColor(113, 118, 123) : new SKColor(83, 100, 113);
            SKColor dividerColor = isDarkTheme ? new SKColor(47, 51, 54) : new SKColor(239, 243, 244);
            SKColor verifiedColor = new(29, 155, 240);

            using var namePaint = new SKPaint
            {
                Color = textColor,
                TextSize = 17,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };

            using var handlePaint = new SKPaint
            {
                Color = subTextColor,
                TextSize = 15,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial")
            };

            using var bodyPaint = new SKPaint
            {
                Color = textColor,
                TextSize = 22,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial")
            };

            using var statNumberPaint = new SKPaint
            {
                Color = textColor,
                TextSize = 15,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };

            // Lay out the text first so the canvas height can fit it
            int logicalWidth = 600;
            float padding = 20;
            float lineHeight = bodyPaint.TextSize + 8;
            var lines = GenerateImageUtils.WrapText(text, logicalWidth - padding * 2, bodyPaint, 12);

            float bodyTop = 100;
            float bodyBottom = bodyTop + lines.Count * lineHeight;
            float timestampY = bodyBottom + 24;
            float dividerY = timestampY + 18;
            float statsY = dividerY + 32;
            int logicalHeight = (int)(statsY + padding);

            int scaleFactor = 2;
            var bitmap = new SKBitmap(logicalWidth * scaleFactor, logicalHeight * scaleFactor);
            using var canvas = new SKCanvas(bitmap);
            canvas.Scale(scaleFactor);
            canvas.Clear(backgroundColor);

            // Avatar
            float avatarRadius = 24;
            GenerateImageUtils.DrawCircularImage(canvas, avatarBitmap, new SKPoint(padding + avatarRadius, padding + avatarRadius + 6), avatarRadius);

            // Display name, verified badge, and handle
            float nameX = padding + avatarRadius * 2 + 12;
            float headerWidth = logicalWidth - nameX - padding;
            displayName = GenerateImageUtils.TruncateText(displayName, verified ? headerWidth - 26 : headerWidth, namePaint);
            handle = GenerateImageUtils.TruncateText($"@{handle}", headerWidth, handlePaint);
            canvas.DrawText(displayName, nameX, 44, namePaint);

            if (verified)
            {
                float badgeRadius = 9;
                float badgeX = nameX + namePaint.MeasureText(displayName) + 4 + badgeRadius;
                float badgeY = 38;

                using var badgePaint = new SKPaint { Color = verifiedColor, IsAntialias = true };
                canvas.DrawCircle(badgeX, badgeY, badgeRadius, badgePaint);

                using var checkPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2.2f,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round
                };
                using var checkPath = new SKPath();
                checkPath.MoveTo(badgeX - 4, badgeY);
                checkPath.LineTo(badgeX - 1, badgeY + 3);
                checkPath.LineTo(badgeX + 4.5f, badgeY - 3.5f);
                canvas.DrawPath(checkPath, checkPaint);
            }

            canvas.DrawText(handle, nameX, 64, handlePaint);

            // Tweet body
            float lineY = bodyTop + bodyPaint.TextSize;
            foreach (var line in lines)
            {
                canvas.DrawText(line, padding, lineY, bodyPaint);
                lineY += lineHeight;
            }

            // Timestamp
            string timestamp = DateTime.UtcNow.ToString("h:mm tt · MMM d, yyyy", CultureInfo.InvariantCulture);
            canvas.DrawText(timestamp, padding, timestampY, handlePaint);

            // Divider
            using var dividerPaint = new SKPaint { Color = dividerColor, StrokeWidth = 1 };
            canvas.DrawLine(padding, dividerY, logicalWidth - padding, dividerY, dividerPaint);

            // Stats
            float statX = padding;
            statX = DrawStat(canvas, GenerateImageUtils.FormatCount(reposts), reposts == 1 ? "Repost" : "Reposts", statX, statsY, statNumberPaint, handlePaint);
            DrawStat(canvas, GenerateImageUtils.FormatCount(likes), likes == 1 ? "Like" : "Likes", statX + 20, statsY, statNumberPaint, handlePaint);

            return bitmap;
        }

        /// <summary>
        /// Draw a bold number followed by a label, returning the x position after the label.
        /// </summary>
        private static float DrawStat(SKCanvas canvas, string number, string label, float x, float y, SKPaint numberPaint, SKPaint labelPaint)
        {
            canvas.DrawText(number, x, y, numberPaint);
            x += numberPaint.MeasureText(number) + 4;
            canvas.DrawText(label, x, y, labelPaint);
            return x + labelPaint.MeasureText(label);
        }
    }
}
