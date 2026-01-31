using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Knightware.Primitives
{
    [TestClass]
    public class ColorTests
    {
        [TestMethod]
        public void ConstructorRgbTest()
        {
            var color = new Color(100, 150, 200);
            Assert.AreEqual(255, color.A);
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
        }

        [TestMethod]
        public void ConstructorArgbTest()
        {
            var color = new Color(128, 100, 150, 200);
            Assert.AreEqual(128, color.A);
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
        }

        [TestMethod]
        public void CopyConstructorTest()
        {
            var original = new Color(128, 100, 150, 200);
            var copy = new Color(original);
            Assert.AreEqual(original.A, copy.A);
            Assert.AreEqual(original.R, copy.R);
            Assert.AreEqual(original.G, copy.G);
            Assert.AreEqual(original.B, copy.B);
        }

        [TestMethod]
        public void ParseRgbStringTest()
        {
            var color = new Color("100,150,200");
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
        }

        [TestMethod]
        public void ParseArgbStringTest()
        {
            var color = new Color("128,100,150,200");
            Assert.AreEqual(128, color.A);
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
        }

        [TestMethod]
        public void ParseInvalidStringTest()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new Color(""));
            Assert.ThrowsExactly<ArgumentException>(() => new Color("100"));
            Assert.ThrowsExactly<ArgumentException>(() => new Color("100,150"));
        }

        [TestMethod]
        public void FromRgbTest()
        {
            var color = Color.FromRgb(100, 150, 200);
            Assert.AreEqual(255, color.A);
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
        }

        [TestMethod]
        public void FromArgbTest()
        {
            var color = Color.FromArgb(128, 100, 150, 200);
            Assert.AreEqual(128, color.A);
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
        }

        [TestMethod]
        public void FromHexStringWithHashTest()
        {
            var color = Color.FromHexString("#FF6496C8");
            Assert.AreEqual(255, color.A);
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
        }

        [TestMethod]
        public void FromHexStringWith0xTest()
        {
            var color = Color.FromHexString("0xFF6496C8");
            Assert.AreEqual(255, color.A);
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
        }

        [TestMethod]
        public void FromHexStringRgbOnlyTest()
        {
            var color = Color.FromHexString("#6496C8");
            Assert.AreEqual(255, color.A);
            Assert.AreEqual(100, color.R);
            Assert.AreEqual(150, color.G);
            Assert.AreEqual(200, color.B);
        }

        [TestMethod]
        public void FromHexStringInvalidLengthTest()
        {
            Assert.ThrowsExactly<Exception>(() => Color.FromHexString("#12345"));
        }

        [TestMethod]
        public void EqualsTest()
        {
            var color1 = new Color(128, 100, 150, 200);
            var color2 = new Color(128, 100, 150, 200);
            var color3 = new Color(128, 100, 150, 201);

            Assert.IsTrue(color1.Equals(color2));
            Assert.IsFalse(color1.Equals(color3));
            Assert.IsTrue(color1.Equals((object)color2));
            Assert.IsFalse(color1.Equals("not a color"));
        }

        [TestMethod]
        public void OperatorEqualsTest()
        {
            var color1 = new Color(128, 100, 150, 200);
            var color2 = new Color(128, 100, 150, 200);
            var color3 = new Color(128, 100, 150, 201);

            Assert.IsTrue(color1 == color2);
            Assert.IsFalse(color1 == color3);
            Assert.IsFalse(color1 != color2);
            Assert.IsTrue(color1 != color3);
        }

        [TestMethod]
        public void ToStringTest()
        {
            var color = new Color(128, 100, 150, 200);
            Assert.AreEqual("R=100, G=150, B=200", color.ToString());
        }

        [TestMethod]
        public void CopyFromTest()
        {
            var source = new Color(128, 100, 150, 200);
            var dest = new Color();
            dest.CopyFrom(source);
            Assert.AreEqual(source.A, dest.A);
            Assert.AreEqual(source.R, dest.R);
            Assert.AreEqual(source.G, dest.G);
            Assert.AreEqual(source.B, dest.B);
        }
    }
}
