using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;


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

            using (var stream = BitmapHelper.GenerateSolidColorBitmap(Primitives.Color.FromArgb(a, r, g, b), width, height))
            {
                stream.Seek(0, SeekOrigin.Begin);

                //Use System.Drawing bitmap to confirm
                using (var bitmap = Bitmap.FromStream(stream))
                {
                    Assert.AreEqual(width, bitmap.Width, "Width was incorrect");
                    Assert.AreEqual(height, bitmap.Height, "Height was incorrect");

                    //TODO:  Test all the pixels for the correct color
                }
            }
        }
    }
}
