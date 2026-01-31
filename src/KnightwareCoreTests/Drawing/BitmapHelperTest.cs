using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace Knightware.Drawing
{
    [TestClass]
    public class BitmapHelperTest
    {
        [TestMethod]
        public void DrawSolidBitmapTest()
        {
            const byte a = 227;
            const byte r = 25;
            const byte g = 88;
            const byte b = 127;
            const int width = 808;
            const int height = 402;

            using var stream = BitmapHelper.GenerateSolidColorBitmap(Primitives.Color.FromArgb(a, r, g, b), width, height);
            stream.Seek(0, SeekOrigin.Begin);

            // Parse BMP header directly (works cross-platform without System.Drawing)
            using var reader = new BinaryReader(stream);

            // BMP Header (14 bytes)
            var signature = new string(reader.ReadChars(2));
            Assert.AreEqual("BM", signature, "Invalid BMP signature");

            var fileSize = reader.ReadInt32();
            reader.ReadInt32(); // Reserved
            var dataOffset = reader.ReadInt32();

            // DIB Header
            var dibHeaderSize = reader.ReadInt32();
            var bmpWidth = reader.ReadInt32();
            var bmpHeight = reader.ReadInt32();

            Assert.AreEqual(width, bmpWidth, "Width was incorrect");
            Assert.AreEqual(height, bmpHeight, "Height was incorrect");

            // Skip to pixel data and verify color
            reader.ReadInt16(); // planes
            var bitsPerPixel = reader.ReadInt16();
            Assert.AreEqual(24, bitsPerPixel, "Expected 24-bit BMP");

            stream.Seek(dataOffset, SeekOrigin.Begin);

            // Read first pixel (BGR order in BMP)
            byte pixelB = reader.ReadByte();
            byte pixelG = reader.ReadByte();
            byte pixelR = reader.ReadByte();

            Assert.AreEqual(b, pixelB, "Blue channel was incorrect");
            Assert.AreEqual(g, pixelG, "Green channel was incorrect");
            Assert.AreEqual(r, pixelR, "Red channel was incorrect");
        }
    }
}
