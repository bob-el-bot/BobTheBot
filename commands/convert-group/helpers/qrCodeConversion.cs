using System;
using System.IO;
using Discord.Interactions;
using QRCoder;
using SkiaSharp;

namespace Commands.Helpers
{
    public class QRCodeConverter
    {
        /// <summary>
        /// The Error Correction Level of the QR code.
        /// </summary>
        public enum ErrorCorrectionLevel
        {
            /// <summary>
            /// Low error correction (7% error recovery)
            /// </summary>
            [ChoiceDisplay("Low (7% error recovery)")]
            L,
            /// <summary>
            /// Medium error correction (15% error recovery)
            /// </summary>
            [ChoiceDisplay("Medium (15% error recovery)")]
            M,
            /// <summary>
            /// Quartile error correction (25% error recovery)
            /// </summary>
            [ChoiceDisplay("Quartile (25% error recovery)")]
            Q,
            /// <summary>
            /// High error correction (30% error recovery)
            /// </summary>
            [ChoiceDisplay("High (30% error recovery)")]
            H
        }

        public static string GetErrorCorrectionLevelDisplay(ErrorCorrectionLevel level)
        {
            // Directly return the string based on the enum value
            return level switch
            {
                ErrorCorrectionLevel.L => "Low (7% error recovery)",
                ErrorCorrectionLevel.M => "Medium (15% error recovery)",
                ErrorCorrectionLevel.Q => "Quartile (25% error recovery)",
                ErrorCorrectionLevel.H => "High (30% error recovery)",
                _ => "Unknown ECC Level",
            };
        }

        // Method to map ECCLevel enum to QRCodeGenerator.ECCLevel
        private static QRCodeGenerator.ECCLevel MapErrorCorrectionLevel(ErrorCorrectionLevel eccLevel)
        {
            return eccLevel switch
            {
                ErrorCorrectionLevel.L => QRCodeGenerator.ECCLevel.L,
                ErrorCorrectionLevel.M => QRCodeGenerator.ECCLevel.M,
                ErrorCorrectionLevel.Q => QRCodeGenerator.ECCLevel.Q,
                ErrorCorrectionLevel.H => QRCodeGenerator.ECCLevel.H,
                _ => QRCodeGenerator.ECCLevel.L
            };
        }

        /// <summary>
        /// Create a QR code PNG image from the specified data.
        /// </summary>
        /// <param name="data">The data to encode in the QR code.</param>
        /// <param name="qrCodeSize">The size of the QR code image (default is 256x256).</param>
        /// <param name="eccLevel">The error correction level for the QR code (default is "L").</param>
        /// <returns>A MemoryStream containing the optimized PNG data for the QR code.</returns>
        public static MemoryStream CreateQRCodePng(string data, int qrCodeSize = 256, ErrorCorrectionLevel eccLevel = ErrorCorrectionLevel.L)
        {
            try
            {
                // Set ECC Level
                QRCodeGenerator.ECCLevel ecc = MapErrorCorrectionLevel(eccLevel);

                QRCodeGenerator qrGenerator = new();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, ecc);

                using PngByteQRCode qrCode = new(qrCodeData);
                byte[] pngData = qrCode.GetGraphic(qrCodeSize);

                if (pngData == null || pngData.Length == 0)
                {
                    throw new InvalidOperationException("Generated QR code PNG byte array is empty.");
                }

                // Compress the PNG using SkiaSharp
                using SKBitmap bitmap = SKBitmap.Decode(pngData) ?? throw new InvalidOperationException("Failed to decode the PNG byte array into an SKBitmap.");

                // Create a memory stream to hold the optimized PNG data
                MemoryStream memoryStream = new();
                using (SKImage skImage = SKImage.FromBitmap(bitmap))
                {
                    // Optimize PNG using SkiaSharp with max compression
                    SKData encodedImage = skImage.Encode(SKEncodedImageFormat.Png, 100); // You can adjust compression here

                    encodedImage.SaveTo(memoryStream);
                }

                // Ensure we return the memory stream at the correct position
                memoryStream.Seek(0, SeekOrigin.Begin);
                Console.WriteLine("Optimized QR Code size: " + memoryStream.Length + " bytes");
                return memoryStream;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }
}
