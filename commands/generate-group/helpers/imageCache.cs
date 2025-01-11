using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;

namespace Commands.Helpers
{
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

        public static SKBitmap GetLikeIcon()
        {
            if (_likeIcon == null)
            {
                _likeIcon = LoadImageFromFile("commands/generate-group/helpers/youtube-like.png");
                _dislikeIcon = PreRenderRotatedImage(_likeIcon, 180);
            }

            return _likeIcon;
        }

        public static SKBitmap GetDislikeIcon()
        {
            if (_dislikeIcon == null)
            {
                GetLikeIcon();
            }
            
            return _dislikeIcon;
        }

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

        private static SKBitmap LoadImageFromFile(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return SKBitmap.Decode(stream);
        }

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
