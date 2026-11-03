using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;

namespace Bob.Commands.Helpers
{
    public static class QuoteImageGenerator
    {
        /// <summary>
        /// Generate a quote card with the author's avatar faded into a black background next to the quote.
        /// </summary>
        /// <param name="quote">The quote text.</param>
        /// <param name="author">The name the quote is attributed to.</param>
        /// <param name="avatarUrl">The URL of the author's avatar image.</param>
        /// <param name="grayscale">Whether to draw the avatar in grayscale.</param>
        /// <param name="customBackgroundColor">A background color to use instead of black, or null.</param>
        /// <param name="customTextColor">A text color to use instead of white, or null. The author line is derived from it.</param>
        /// <returns>A Task representing the asynchronous operation that returns the generated SKBitmap image. Returns null if the given avatarUrl gets a null image.</returns>
        public static async Task<SKBitmap> GenerateQuoteImage(string quote, string author, string avatarUrl, bool grayscale, SKColor? customBackgroundColor = null, SKColor? customTextColor = null)
        {
            SKBitmap avatarBitmap = await ImageCache.GetImageFromUrl(avatarUrl);
            if (avatarBitmap == null)
            {
                return null;
            }

            int scaleFactor = 2;
            int width = 800;
            int height = 400;
            var bitmap = new SKBitmap(width * scaleFactor, height * scaleFactor);
            using var canvas = new SKCanvas(bitmap);
            canvas.Scale(scaleFactor);

            SKColor backgroundColor = customBackgroundColor ?? SKColors.Black;
            SKColor textColor = customTextColor ?? SKColors.White;
            SKColor authorColor = customTextColor?.WithAlpha(170) ?? new SKColor(170, 170, 170);
            canvas.Clear(backgroundColor);

            // Avatar on the left, filling the full height
            var avatarRect = new SKRect(0, 0, height, height);
            using (var avatarPaint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true })
            {
                if (grayscale)
                {
                    avatarPaint.ColorFilter = GenerateImageUtils.CreateGrayscaleFilter();
                }

                GenerateImageUtils.DrawImageCover(canvas, avatarBitmap, avatarRect, avatarPaint);
            }

            // Fade the avatar into the background
            using (var fadePaint = new SKPaint())
            {
                fadePaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(height * 0.35f, 0),
                    new SKPoint(height, 0),
                    [backgroundColor.WithAlpha(0), backgroundColor],
                    SKShaderTileMode.Clamp);
                canvas.DrawRect(avatarRect, fadePaint);
            }

            // Fit the quote into the right side, shrinking the font until it fits
            float textLeft = height;
            float textWidth = width - textLeft - 40;
            float textCenterX = textLeft + textWidth / 2;
            float maxTextHeight = height - 120;

            using var quotePaint = new SKPaint
            {
                Color = textColor,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Georgia", SKFontStyle.Italic)
            };

            string quoted = $"“{quote}”";
            List<string> lines;
            float lineHeight;
            float fontSize = 40;
            while (true)
            {
                quotePaint.TextSize = fontSize;
                lineHeight = fontSize * 1.25f;
                lines = GenerateImageUtils.WrapText(quoted, textWidth, quotePaint, (int)(maxTextHeight / lineHeight), out bool truncated);

                if (!truncated || fontSize <= 16)
                {
                    break;
                }

                fontSize -= 2;
            }

            using var authorPaint = new SKPaint
            {
                Color = authorColor,
                TextSize = 20,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Georgia", SKFontStyle.Italic)
            };

            // Vertically center the quote and author together
            float blockHeight = lines.Count * lineHeight + 20 + authorPaint.TextSize;
            float y = (height - blockHeight) / 2 + quotePaint.TextSize;
            foreach (var line in lines)
            {
                canvas.DrawText(line, textCenterX, y, quotePaint);
                y += lineHeight;
            }

            canvas.DrawText(GenerateImageUtils.TruncateText($"- {author}", textWidth, authorPaint), textCenterX, y + 20, authorPaint);

            return bitmap;
        }
    }
}
