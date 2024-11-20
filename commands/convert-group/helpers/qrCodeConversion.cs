using System;
using System.IO;
using System.Collections.Generic;
using Discord.Interactions;
using QRCoder;
using SkiaSharp;
using System.Text;

namespace Commands.Helpers
{
    public class QRCodeConverter
    {
        /// <summary>
        /// The Error Correction Level of the QR code.
        /// </summary>
        public enum ErrorCorrectionLevel
        {
            /// <summary>Low error correction (7% error recovery)</summary>
            [ChoiceDisplay("Low (7% error recovery)")]
            L,
            /// <summary>Medium error correction (15% error recovery)</summary>
            [ChoiceDisplay("Medium (15% error recovery)")]
            M,
            /// <summary>Quartile error correction (25% error recovery)</summary>
            [ChoiceDisplay("Quartile (25% error recovery)")]
            Q,
            /// <summary>High error correction (30% error recovery)</summary>
            [ChoiceDisplay("High (30% error recovery)")]
            H
        }

        // Mapped values for error correction levels
        private static readonly Dictionary<ErrorCorrectionLevel, string> ErrorCorrectionDisplayMap = new()
        {
            { ErrorCorrectionLevel.L, "Low (7% error recovery)" },
            { ErrorCorrectionLevel.M, "Medium (15% error recovery)" },
            { ErrorCorrectionLevel.Q, "Quartile (25% error recovery)" },
            { ErrorCorrectionLevel.H, "High (30% error recovery)" }
        };

        private static readonly Dictionary<ErrorCorrectionLevel, QRCodeGenerator.ECCLevel> ErrorCorrectionMap = new()
        {
            { ErrorCorrectionLevel.L, QRCodeGenerator.ECCLevel.L },
            { ErrorCorrectionLevel.M, QRCodeGenerator.ECCLevel.M },
            { ErrorCorrectionLevel.Q, QRCodeGenerator.ECCLevel.Q },
            { ErrorCorrectionLevel.H, QRCodeGenerator.ECCLevel.H }
        };

        // Predefined maximum payload sizes (bytes) for versions 1 to 40 and error correction levels (L, M, Q, H)
        public static readonly int[,] MaxPayloadSizes = new int[,]
        {
            { 17, 14, 11, 7 }, { 32, 26, 20, 14 }, { 53, 42, 32, 24 },
            { 78, 62, 46, 34 }, { 106, 84, 60, 44 }, { 134, 106, 82, 58 },
            { 154, 122, 94, 66 }, { 182, 140, 106, 82 }, { 216, 168, 130, 98 },
            { 240, 192, 154, 118 }, { 290, 224, 174, 134 }, { 334, 260, 202, 154 },
            { 382, 290, 222, 174 }, { 430, 338, 254, 202 }, { 461, 365, 285, 223 },
            { 523, 415, 325, 253 }, { 589, 453, 357, 285 }, { 647, 511, 382, 322 },
            { 721, 563, 437, 355 }, { 795, 617, 451, 365 }, { 869, 669, 511, 397 },
            { 939, 723, 535, 405 }, { 1043, 795, 595, 445 }, { 1115, 845, 625, 465 },
            { 1193, 901, 658, 485 }, { 1273, 961, 698, 505 }, { 1367, 1025, 742, 525 },
            { 1465, 1091, 784, 545 }, { 1528, 1153, 812, 565 }, { 1628, 1229, 868, 605 },
            { 1732, 1303, 908, 625 }, { 1840, 1379, 958, 645 }, { 1952, 1455, 1010, 665 },
            { 2068, 1531, 1062, 685 }, { 2188, 1609, 1118, 705 }, { 2303, 1681, 1170, 725 },
            { 2421, 1759, 1222, 745 }, { 2541, 1837, 1276, 765 }, { 2663, 1914, 1322, 785 },
            { 2789, 1992, 1370, 805 }
        };

        /// <summary>
        /// Get the error message for the payload size exceeding the maximum size of a QR code.
        /// </summary>
        /// <param name="payloadSize">The size of the payload in bytes.</param>
        /// <param name="errorCorrectionLevel">The error correction level.</param>
        /// <returns>The error message for the payload size exceeding the maximum size of a QR code.</returns>
        public static string GetPayloadSizeErrorMessage(int payloadSize, ErrorCorrectionLevel errorCorrectionLevel)
        {
            int maxAllowedSize = MaxPayloadSizes[39, (int)MapErrorCorrectionLevel(errorCorrectionLevel)];
            int sizeDifference = payloadSize - maxAllowedSize;
            int charEstimation = GetCharacterEstimation(sizeDifference);

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("❌ The QR Code **could not** be generated because the given content exceeds the maximum size of a QR code.");

            if (errorCorrectionLevel > ErrorCorrectionLevel.L)
            {
                messageBuilder.AppendLine("- Try lowering the error correction level.");
            }

            messageBuilder.AppendLine($"- Try shortening the content by at least **{sizeDifference} bytes** (or approximately **{charEstimation} characters**).");

            return messageBuilder.ToString();
        }

        /// <summary>
        /// Get the estimated number of characters that can be encoded in the QR code.
        /// </summary>
        /// <param name="sizeDifference">The size difference between the data and the maximum payload size.</param>
        /// <returns>The estimated number of characters that can be encoded in the QR code.</returns>
        public static int GetCharacterEstimation(int sizeDifference)
        {
            // Estimate the average number of bytes per character for UTF-8 encoding
            // Assume an average of 1.5 bytes per character as a rough estimate (since UTF-8 characters range from 1 to 3 bytes)
            double averageBytesPerCharacter = 1.5;
            return (int)(sizeDifference / averageBytesPerCharacter);
        }

        /// <summary>
        /// Get the display string for the error correction level.
        /// </summary>
        /// <param name="level">The error correction level.</param>
        /// <returns>The display string for the error correction level.</returns>
        public static string GetErrorCorrectionLevelDisplay(ErrorCorrectionLevel level)
        {
            return ErrorCorrectionDisplayMap.TryGetValue(level, out var display) ? display : "Unknown ECC Level";
        }

        /// <summary>
        /// Map the error correction level from the enum to the QR code library.
        /// </summary>
        /// <param name="eccLevel">The error correction level.</param>
        /// <returns>The mapped error correction level.</returns>
        public static QRCodeGenerator.ECCLevel MapErrorCorrectionLevel(ErrorCorrectionLevel eccLevel)
        {
            return ErrorCorrectionMap.GetValueOrDefault(eccLevel, QRCodeGenerator.ECCLevel.L);
        }

        /// <summary>
        /// Returns the version of QR code that can fit the given data size with the specified error correction level.
        /// </summary>
        /// <param name="dataSize">The size of the data in bytes.</param>
        /// <param name="eccLevel">The error correction level.</param>
        /// <returns>The version of QR code that can fit the data size, or -1 if the data is too large.</returns>
        public static int GetSuitableVersion(int dataSize, ErrorCorrectionLevel eccLevel)
        {
            int eccIndex = (int)MapErrorCorrectionLevel(eccLevel);
            for (int version = 0; version < MaxPayloadSizes.GetLength(0); version++)
            {
                if (dataSize <= MaxPayloadSizes[version, eccIndex])
                {
                    return version + 1;
                }
            }
            return -1;
        }

        /// <summary>
        /// Creates a QR Code image as a PNG in memory.
        /// </summary>
        /// <param name="data">The data to encode in the QR code.</param>
        /// <param name="eccLevel">The error correction level.</param>
        /// <returns>The QR code image as a PNG in memory.</returns>
        public static MemoryStream CreateQRCodePng(string data, int suitableVersion, ErrorCorrectionLevel eccLevel = ErrorCorrectionLevel.L)
        {
            try
            {
                // Map the error correction level
                var ecc = MapErrorCorrectionLevel(eccLevel);

                // Initialize the QR code generator
                var qrGenerator = new QRCodeGenerator();

                // Create the QR code data with the appropriate ECC level and version
                var qrCodeData = qrGenerator.CreateQrCode(data, ecc, requestedVersion: suitableVersion);

                int moduleCount = qrCodeData.ModuleMatrix.Count;
                int imageSize = 400;
                int moduleSize = imageSize / moduleCount;
                int margin = (imageSize - (moduleSize * moduleCount)) / 2;

                // Create the SkiaSharp bitmap
                using var bitmap = new SKBitmap(imageSize, imageSize);
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.White);
                    var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true, Style = SKPaintStyle.Fill };

                    // Loop through the QR code data and draw it on the canvas
                    for (int x = 0; x < moduleCount; x++)
                    {
                        for (int y = 0; y < moduleCount; y++)
                        {
                            if (qrCodeData.ModuleMatrix[y][x])
                            {
                                canvas.DrawRect((x * moduleSize) + margin, (y * moduleSize) + margin, moduleSize, moduleSize, paint);
                            }
                        }
                    }
                }

                MemoryStream memoryStream = new();
                using (var skImage = SKImage.FromBitmap(bitmap))
                {
                    var encodedImage = skImage.Encode(SKEncodedImageFormat.Png, 90);
                    encodedImage.SaveTo(memoryStream);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error generating QR code: " + ex.Message);
            }
        }
    }
}
