using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;

namespace Bob.Commands.Helpers
{
    /// <summary>
    /// A cache for images downloaded from URLs.
    /// </summary>
    public static class ImageCache
    {
        private static SKBitmap _likeIcon;
        private static SKBitmap _dislikeIcon;
        private static readonly MemoryCache Cache = new(new MemoryCacheOptions());
        private static readonly ConcurrentDictionary<string, Task<SKBitmap>> OnGoingDownloads = new();

        static ImageCache()
        {
            // Preload the like and dislike icons
            GetLikeIcon();
            GetDislikeIcon();
        }

        /// <summary>
        /// Gets the like icon.
        /// </summary>
        public static SKBitmap GetLikeIcon()
        {
            if (_likeIcon == null)
            {
                _likeIcon = LoadImageFromFile("commands/generate-group/helpers/youtube-like.png");
                _dislikeIcon = PreRenderRotatedImage(_likeIcon, 180);
            }

            return _likeIcon;
        }

        /// <summary>
        /// Gets the dislike icon.
        /// </summary>
        public static SKBitmap GetDislikeIcon()
        {
            if (_dislikeIcon == null)
            {
                GetLikeIcon();
            }

            return _dislikeIcon;
        }

        /// <summary>
        /// Gets an image from a URL, caching it for 5 minutes.
        /// </summary>
        /// <param name="url">The URL to download the image from.</param>
        /// <returns>The downloaded image.</returns>
        public static async Task<SKBitmap> GetImageFromUrl(string url)
        {
            // Check if the image is already cached
            if (Cache.TryGetValue(url, out var cachedBitmap))
            {
                // Update the last accessed time and return the cached image
                Cache.Set(url, cachedBitmap, DateTime.Now.AddMinutes(5));
                
                return (SKBitmap)cachedBitmap;
            }

            // Check if a download is already in progress for this URL
            var downloadTask = OnGoingDownloads.GetOrAdd(url, async _ =>
            {
                try
                {
                    // Perform the download
                    byte[] imageBytes = await new HttpClient().GetByteArrayAsync(url);
                    using var stream = new MemoryStream(imageBytes);
                    var bitmap = SKBitmap.Decode(stream);

                    // Cache the image and mark the download as complete
                    Cache.Set(url, bitmap, DateTime.Now.AddMinutes(5));
                    
                    return bitmap;
                }
                catch
                {
                    // Handle download failure
                    return null;
                }
                finally
                {
                    // Clean up the task after completion
                    OnGoingDownloads.TryRemove(url, out Task<SKBitmap> _);
                }
            });

            // Await the download task and return the result
            return await downloadTask;
        }

        /// <summary>
        /// Loads an image from a file.
        /// </summary>
        /// <param name="filePath">The path to the image file.</param>
        /// <returns>The loaded image.</returns>
        private static SKBitmap LoadImageFromFile(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return SKBitmap.Decode(stream);
        }

        /// <summary>
        /// Pre-renders a rotated image.
        /// </summary>
        /// <param name="originalBitmap">The original image to rotate.</param>
        /// <param name="angle">The angle to rotate the image by.</param>
        /// <returns>The rotated image.</returns>
        private static SKBitmap PreRenderRotatedImage(SKBitmap originalBitmap, float angle)
        {
            var rotatedBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height);
            using var canvas = new SKCanvas(rotatedBitmap);
            canvas.Translate(originalBitmap.Width / 2f, originalBitmap.Height / 2f);
            canvas.RotateDegrees(angle);
            canvas.DrawBitmap(originalBitmap, -originalBitmap.Width / 2f, -originalBitmap.Height / 2f);
            return rotatedBitmap;
        }
    }
}
