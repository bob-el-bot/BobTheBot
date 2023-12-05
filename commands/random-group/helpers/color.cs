using System;
using System.IO;
using SkiaSharp;

namespace Commands.Helpers
{
    public static class ColorPreview
    {
        public static MemoryStream CreateColorImage(int width, int height, string hex)
        {
            SKColor color = HexToSKColor(hex);

            MemoryStream stream = new MemoryStream();

            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                using (var canvas = surface.Canvas)
                {
                    using (var paint = new SKPaint { Color = color })
                    {
                        canvas.DrawRect(new SKRect(0, 0, width, height), paint);
                    }
                }

                using (var image = surface.Snapshot())
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var imageStream = data.AsStream())
                {
                    imageStream.CopyTo(stream);
                }
            }

            // Reset the position to the beginning of the MemoryStream before returning it
            stream.Position = 0;

            return stream;
        }

        private static SKColor HexToSKColor(string hex)
        {
            hex = hex.TrimStart('#');
            int colorValue = Convert.ToInt32(hex, 16);
            return new SKColor((byte)((colorValue >> 16) & 0xFF), (byte)((colorValue >> 8) & 0xFF), (byte)(colorValue & 0xFF));
        }
    }
}
