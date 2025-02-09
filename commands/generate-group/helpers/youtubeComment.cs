using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Bob.Commands.Helpers
{
    public static class YouTubeCommentImageGenerator
    {
        /// <summary>
        /// The theme of the comment image (light or dark).
        /// </summary>
        public enum Theme
        {
            Light,
            Dark
        }

        /// <summary>
        /// The unit of time for the timeAgo value (second, minute, hour, day, week, month, year).
        /// </summary>
        public enum TimeUnit
        {
            Second,
            Minute,
            Hour,
            Day,
            Week,
            Month,
            Year
        }

        /// <summary>
        /// Generate a YouTube comment image with the specified details.
        /// </summary>
        /// <param name="username">The username of the commenter.</param>
        /// <param name="avatarUrl">The URL of the commenter's avatar image.</param>
        /// <param name="comment">The comment text.</param>
        /// <param name="timeAgo">The time since the comment was posted.</param>
        /// <param name="timeUnit">The unit of time for the timeAgo value.</param>
        /// <param name="likesCount">The number of likes on the comment.</param>
        /// <param name="theme">The theme of the comment image (light or dark).</param>
        /// <returns>A Task representing the asynchronous operation that returns the generated SKBitmap image. Returns null if the given avatarUrl gets a null image.</returns>
        public static async Task<SKBitmap> GenerateYouTubeCommentImage(string username, string avatarUrl, string comment, int timeAgo, TimeUnit timeUnit, int likesCount, Theme theme)
        {
            // Load the avatar and like icon images from URLs and file paths
            SKBitmap avatarBitmap = await ImageCache.GetImageFromUrl(avatarUrl);
            if (avatarBitmap == null)
            {
                return null;
            }

            SKBitmap likeIconBitmap = ImageCache.GetLikeIcon();
            SKBitmap dislikeIconBitmap = ImageCache.GetDislikeIcon();

            // Create a high-resolution canvas
            int scaleFactor = 2; // Scale factor for high resolution
            int width = 800 * scaleFactor;
            int height = 160 * scaleFactor;
            SKBitmap bitmap = new(width, height);
            SKCanvas canvas = new(bitmap);
            canvas.Scale(scaleFactor); // Apply scale for sharper output

            // Determine the theme based on the enum
            bool isDarkTheme = theme == Theme.Dark;

            // Define theme colors
            SKColor backgroundColor = isDarkTheme ? new SKColor(15, 15, 15) : SKColors.White;
            SKColor textColor = isDarkTheme ? SKColors.White : SKColors.Black;
            SKColor subTextColor = isDarkTheme ? new SKColor(96, 96, 96) : new SKColor(170, 170, 170);

            canvas.Clear(backgroundColor); // Set background color

            // Draw the circular avatar
            int avatarSize = 50; // Logical size (not scaled)
            SKPoint avatarCenter = new(45, 45);
            canvas.Save();
            using (var avatarPath = new SKPath())
            {
                float radius = avatarSize / 2;
                avatarPath.AddCircle(avatarCenter.X, avatarCenter.Y, radius);
                canvas.ClipPath(avatarPath, antialias: true);

                float left = avatarCenter.X - radius;
                float top = avatarCenter.Y - radius;
                float right = avatarCenter.X + radius;
                float bottom = avatarCenter.Y + radius;

                canvas.DrawBitmap(avatarBitmap, new SKRect(left, top, right, bottom), new SKPaint
                {
                    FilterQuality = SKFilterQuality.High,
                    IsAntialias = true
                });
            }
            canvas.Restore();

            // Set up text paints
            SKPaint usernamePaint = new()
            {
                Color = textColor,
                TextSize = 18,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };

            SKPaint timePaint = new()
            {
                Color = subTextColor,
                TextSize = 18,
                IsAntialias = true
            };

            SKPaint likesPaint = new()
            {
                Color = subTextColor,
                TextSize = 16,
                IsAntialias = true
            };

            SKPaint commentPaint = new()
            {
                Color = textColor,
                TextSize = 18,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial")
            };

            // Draw the username
            float usernameX = 80;
            float usernameY = 40;
            canvas.DrawText(username, usernameX, usernameY, usernamePaint);

            // Draw the "time ago" text
            float timeX = usernameX + usernamePaint.MeasureText(username) + 10;
            canvas.DrawText(GetTimeAgoString(timeAgo, timeUnit), timeX, usernameY, timePaint);

            // Wrap and draw the comment
            List<string> wrappedComment = WrapText(comment, (width / scaleFactor) - 100, commentPaint);
            float commentY = 80;
            foreach (var line in wrappedComment)
            {
                canvas.DrawText(line, 80, commentY, commentPaint);
                commentY += commentPaint.TextSize + 5;
            }

            // Draw the like icon and count
            int likeIconSize = 20; // Size of the like icon
            float likeIconX = 80; // Position of the like icon
            float likeIconY = height / scaleFactor - likeIconSize - 15; // Near the bottom left, with some padding

            // Define a color filter to match the subtext color
            using (var paint = new SKPaint())
            {
                paint.ColorFilter = SKColorFilter.CreateBlendMode(subTextColor, SKBlendMode.SrcIn);

                // Draw the like icon with the color filter applied
                canvas.DrawBitmap(likeIconBitmap, new SKRect(likeIconX, likeIconY, likeIconX + likeIconSize, likeIconY + likeIconSize), paint);
            }

            // Draw the like count next to the icon
            float likesTextX = likeIconX + likeIconSize + 5; // Add some space between the icon and text
            float likesTextY = likeIconY + likeIconSize - 5; // Align with the icon
            string formattedLikes = FormatLikeCount(likesCount);
            canvas.DrawText(formattedLikes, likesTextX, likesTextY, likesPaint);

            // Draw the dislike icon
            float dislikeIconX = likeIconX + 74; // Fixed spacing from the like icon
            canvas.DrawBitmap(dislikeIconBitmap, new SKRect(dislikeIconX, likeIconY, dislikeIconX + likeIconSize, likeIconY + likeIconSize), new SKPaint
            {
                ColorFilter = SKColorFilter.CreateBlendMode(subTextColor, SKBlendMode.SrcIn)
            });

            // Draw the "Reply" text
            SKPaint replyPaint = new()
            {
                Color = textColor,
                TextSize = 16,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };

            // Position the "REPLY" text
            float replyX = dislikeIconX + likeIconSize + 30; // Add spacing between the dislike icon and "REPLY"
            float replyY = likeIconY + likeIconSize - 5; // Align with the dislike icon
            canvas.DrawText("Reply", replyX, replyY, replyPaint);

            // Return the high-resolution bitmap
            return bitmap;
        }

        /// <summary>
        /// Wrap text to fit within a specified width.
        /// </summary>
        /// <param name="text">The text to wrap.</param>
        /// <param name="maxWidth">The maximum width for each line.</param>
        /// <param name="paint">The SKPaint object used to measure the text width.</param>
        /// <returns>A list of wrapped lines.</returns>
        private static List<string> WrapText(string text, float maxWidth, SKPaint paint)
        {
            var lines = new List<string>();
            var words = text.Split(' '); // Split text into words
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;

                // Check if the current line fits within the max width
                if (paint.MeasureText(testLine) <= maxWidth)
                {
                    currentLine.Append(string.IsNullOrEmpty(currentLine.ToString()) ? word : " " + word);
                }
                else
                {
                    // Add the current line to the list and start a new one
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    currentLine.Append(word);

                    // If the third line is about to be created, truncate the second line
                    if (lines.Count == 2)
                    {
                        string secondLine = lines[1]; // Current second line
                        while (paint.MeasureText(secondLine + "...") > maxWidth)
                        {
                            secondLine = secondLine[..^1]; // Remove one character at a time
                        }

                        lines[1] = secondLine + "..."; // Replace the second line with truncated text
                        return lines; // Stop processing after two lines
                    }
                }
            }

            // Add any remaining text
            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }

            // Ensure only two lines, with ellipsis if necessary
            if (lines.Count == 2 && paint.MeasureText(lines[1]) > maxWidth)
            {
                string secondLine = lines[1];
                while (paint.MeasureText(secondLine + "...") > maxWidth)
                {
                    secondLine = secondLine[..^1];
                }

                lines[1] = secondLine + "...";
            }

            return lines;
        }

        /// <summary>
        /// Format a like count to a human-readable format. For example, 1234 becomes "1.2K", 12345 becomes "12K", etc.
        /// </summary>
        /// <param name="count">The like count to format.</param>
        /// <returns>The formatted like count.</returns>
        private static string FormatLikeCount(int count)
        {
            if (count == 0)
            {
                return "";
            }
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
                return (count / 1000000).ToString("0") + "M"; // Whole number for millions
            }
        }

        /// <summary>
        /// Get a human-readable string for a time value and unit. For example, 1 Hour Ago, 5 Minutes Ago, etc.
        /// </summary>
        /// <param name="timeValue">The amount of time.</param>
        /// <param name="unit">The unit of time.</param>
        /// <returns>The formatted time string.</returns>
        private static string GetTimeAgoString(int timeValue, TimeUnit unit)
        {
            // Pluralize the unit based on the timeValue
            string unitString = timeValue == 1 ? unit.ToString().ToLower() : $"{unit.ToString().ToLower()}s";

            // Return the formatted string
            return $"{timeValue} {unitString} ago";
        }

        /// <summary>
        /// Save an SKBitmap image to a file.
        /// </summary>
        /// <param name="bitmap">The SKBitmap image to save.</param>
        /// <param name="path">The file path to save the image to.</param>
        public static void SaveImage(SKBitmap bitmap, string path)
        {
            using var stream = File.OpenWrite(path);
            bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        }
    }
}