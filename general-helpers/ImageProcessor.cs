using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using System.IO;

public static class ImageProcessor
{
    /// <summary>
    /// Converts image data to an animated WebP format if possible.
    /// </summary>
    /// <param name="imageData">The byte array of the input image.</param>
    /// <param name="quality">Compression quality (1-100).</param>
    /// <returns>Byte array of the WebP image.</returns>
    public static byte[] ConvertToAnimatedWebP(byte[] imageData, int quality = 75)
    {
        using var image = Image.Load(imageData);
        using var outputStream = new MemoryStream();

        var encoder = new WebpEncoder
        {
            Quality = quality,
            Method = WebpEncodingMethod.Level4
        };

        image.SaveAsWebp(outputStream, encoder);
        return outputStream.ToArray();
    }
}