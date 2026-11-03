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
        private static readonly HttpClient Http = new();

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
                Cache.Set(url, cachedBitmap, CreateEntryOptions());
                
                return (SKBitmap)cachedBitmap;
            }

            // Check if a download is already in progress for this URL
            var downloadTask = OnGoingDownloads.GetOrAdd(url, async _ =>
            {
                try
                {
                    // Perform the download
                    byte[] imageBytes = await Http.GetByteArrayAsync(url);
                    using var stream = new MemoryStream(imageBytes);
                    var bitmap = SKBitmap.Decode(stream);

                    // Cache the image and mark the download as complete
                    Cache.Set(url, bitmap, CreateEntryOptions());
                    
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
        /// Creates cache options that expire an image after 5 minutes and free its native memory once evicted.
        /// </summary>
        private static MemoryCacheEntryOptions CreateEntryOptions()
        {
            return new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(5)
            }.RegisterPostEvictionCallback(static (_, value, reason, _) =>
            {
                // Replaced means the same bitmap was re-cached on access, so it is still in use
                if (reason == EvictionReason.Replaced || value is not SKBitmap bitmap)
                {
                    return;
                }

                // Delay disposal so a command that grabbed the bitmap right before it expired can finish drawing with it
                _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => bitmap.Dispose());
            });
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
