using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Bob.Commands.Helpers
{
    /// <summary>
    /// Drawing utilities shared by the /generate image generators.
    /// </summary>
    public static class GenerateImageUtils
    {
        private const string Ellipsis = "...";

        /// <summary>
        /// Wrap text to fit within a specified width, honoring line breaks and splitting words that are too long for a single line.
        /// </summary>
        /// <param name="text">The text to wrap.</param>
        /// <param name="maxWidth">The maximum width for each line.</param>
        /// <param name="paint">The SKPaint object used to measure the text width.</param>
        /// <param name="maxLines">The maximum number of lines. The last line is truncated with an ellipsis if the text does not fit.</param>
        /// <returns>A list of wrapped lines.</returns>
        public static List<string> WrapText(string text, float maxWidth, SKPaint paint, int maxLines)
        {
            return WrapText(text, maxWidth, paint, maxLines, out _);
        }

        /// <inheritdoc cref="WrapText(string, float, SKPaint, int)"/>
        /// <param name="truncated">Whether the text had to be truncated to fit within maxLines.</param>
        public static List<string> WrapText(string text, float maxWidth, SKPaint paint, int maxLines, out bool truncated)
        {
            var lines = new List<string>();
            truncated = false;

            foreach (var paragraph in text.Replace("\r", "").Split('\n'))
            {
                var currentLine = new StringBuilder();

                foreach (var rawWord in paragraph.Split(' '))
                {
                    string word = rawWord;
                    string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;

                    if (paint.MeasureText(testLine) <= maxWidth)
                    {
                        currentLine.Clear().Append(testLine);
                        continue;
                    }

                    if (paint.MeasureText(word) > maxWidth)
                    {
                        // Words too long for any line fill the rest of the current line before breaking
                        word = testLine;
                        currentLine.Clear();
                    }
                    else if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }

                    // Break up words that are wider than a full line
                    while (paint.MeasureText(word) > maxWidth && lines.Count <= maxLines)
                    {
                        int fit = SafeSplitIndex(word, (int)paint.BreakText(word, maxWidth));
                        lines.Add(word[..fit]);
                        word = word[fit..];
                    }

                    if (lines.Count > maxLines)
                    {
                        break;
                    }

                    currentLine.Append(word);
                }

                lines.Add(currentLine.ToString());

                if (lines.Count > maxLines)
                {
                    truncated = true;
                    break;
                }
            }

            if (lines.Count > maxLines)
            {
                truncated = true;
                lines.RemoveRange(maxLines, lines.Count - maxLines);
            }

            if (truncated)
            {
                string lastLine = lines[^1];
                while (lastLine.Length > 0 && paint.MeasureText(lastLine + Ellipsis) > maxWidth)
                {
                    lastLine = lastLine[..SafeSplitIndex(lastLine, lastLine.Length - 1, allowEmpty: true)];
                }

                lines[^1] = lastLine.TrimEnd() + Ellipsis;
            }

            return lines;
        }

        /// <summary>
        /// Fit text onto a single line, joining line breaks with spaces and truncating with an ellipsis if it is too wide.
        /// </summary>
        /// <param name="text">The text to fit.</param>
        /// <param name="maxWidth">The maximum width of the line.</param>
        /// <param name="paint">The SKPaint object used to measure the text width.</param>
        /// <returns>The text, truncated if necessary.</returns>
        public static string TruncateText(string text, float maxWidth, SKPaint paint)
        {
            return WrapText(text.ReplaceLineEndings(" "), maxWidth, paint, 1)[0];
        }

        /// <summary>
        /// Adjust a split index so it never cuts a surrogate pair (e.g. an emoji) in half.
        /// </summary>
        /// <param name="text">The text being split.</param>
        /// <param name="index">The desired split index.</param>
        /// <param name="allowEmpty">Whether a split index of 0 is allowed. If not, at least one character is always kept.</param>
        private static int SafeSplitIndex(string text, int index, bool allowEmpty = false)
        {
            index = Math.Clamp(index, allowEmpty ? 0 : 1, text.Length);
            if (index > 0 && index < text.Length && char.IsHighSurrogate(text[index - 1]) && char.IsLowSurrogate(text[index]))
            {
                // Step back before the pair, or past it if stepping back would leave nothing
                index = index - 1 > 0 || allowEmpty ? index - 1 : index + 1;
            }

            return index;
        }

        /// <summary>
        /// Draw an image inside a circle.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="image">The image to draw.</param>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        public static void DrawCircularImage(SKCanvas canvas, SKBitmap image, SKPoint center, float radius)
        {
            canvas.Save();
            using (var path = new SKPath())
            {
                path.AddCircle(center.X, center.Y, radius);
                canvas.ClipPath(path, antialias: true);
                DrawImageCover(canvas, image, new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius));
            }
            canvas.Restore();
        }

        /// <summary>
        /// Draw an image so that it fills the destination rectangle, cropping the image instead of stretching it.
        /// </summary>
        /// <param name="canvas">The canvas to draw on.</param>
        /// <param name="image">The image to draw.</param>
        /// <param name="dest">The rectangle to fill.</param>
        /// <param name="paint">An optional paint, e.g. for applying a color filter.</param>
        public static void DrawImageCover(SKCanvas canvas, SKBitmap image, SKRect dest, SKPaint paint = null)
        {
            float imageAspect = (float)image.Width / image.Height;
            float destAspect = dest.Width / dest.Height;

            SKRect source;
            if (imageAspect > destAspect)
            {
                // Image is wider than the destination, so crop the sides
                float cropWidth = image.Height * destAspect;
                float x = (image.Width - cropWidth) / 2;
                source = new SKRect(x, 0, x + cropWidth, image.Height);
            }
            else
            {
                // Image is taller than the destination, so crop the top and bottom
                float cropHeight = image.Width / destAspect;
                float y = (image.Height - cropHeight) / 2;
                source = new SKRect(0, y, image.Width, y + cropHeight);
            }

            using var defaultPaint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true };
            canvas.DrawBitmap(image, source, dest, paint ?? defaultPaint);
        }

        /// <summary>
        /// Create a color filter that converts an image to grayscale.
        /// </summary>
        public static SKColorFilter CreateGrayscaleFilter()
        {
            return SKColorFilter.CreateColorMatrix(
            [
                0.21f, 0.72f, 0.07f, 0, 0,
                0.21f, 0.72f, 0.07f, 0, 0,
                0.21f, 0.72f, 0.07f, 0, 0,
                0,     0,     0,     1, 0
            ]);
        }

        /// <summary>
        /// Format a count to a human-readable format. For example, 1234 becomes "1.2K", 12345 becomes "12K", etc.
        /// </summary>
        /// <param name="count">The count to format.</param>
        /// <returns>The formatted count.</returns>
        public static string FormatCount(int count)
        {
            if (count < 1000)
            {
                return count.ToString();
            }
            else if (count < 10000)
            {
                return (count / 1000.0).ToString("0.#") + "K";
            }
            else if (count < 1000000)
            {
                return (count / 1000).ToString("0") + "K";
            }
            else if (count < 10000000)
            {
                return (count / 1000000.0).ToString("0.#") + "M";
            }
            else
            {
                return (count / 1000000).ToString("0") + "M";
            }
        }
    }
}
