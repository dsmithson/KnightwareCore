using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knightware.Primitives
{
    [TestClass]
    public class RectangleTests
    {
        [TestMethod]
        public void ConstructorTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            Assert.AreEqual(10, rect.X);
            Assert.AreEqual(20, rect.Y);
            Assert.AreEqual(100, rect.Width);
            Assert.AreEqual(200, rect.Height);
        }

        [TestMethod]
        public void CopyConstructorTest()
        {
            var original = new Rectangle(10, 20, 100, 200);
            var copy = new Rectangle(original);
            Assert.AreEqual(original.X, copy.X);
            Assert.AreEqual(original.Y, copy.Y);
            Assert.AreEqual(original.Width, copy.Width);
            Assert.AreEqual(original.Height, copy.Height);
        }

        [TestMethod]
        public void EmptyTest()
        {
            var empty = Rectangle.Empty;
            Assert.IsTrue(empty.IsEmpty);
            Assert.AreEqual(0, empty.X);
            Assert.AreEqual(0, empty.Y);
            Assert.AreEqual(0, empty.Width);
            Assert.AreEqual(0, empty.Height);
        }

        [TestMethod]
        public void IsEmptyFalseTest()
        {
            var rect = new Rectangle(1, 0, 0, 0);
            Assert.IsFalse(rect.IsEmpty);
        }

        [TestMethod]
        public void TopLeftRightBottomTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            Assert.AreEqual(20, rect.Top);
            Assert.AreEqual(10, rect.Left);
            Assert.AreEqual(110, rect.Right);
            Assert.AreEqual(220, rect.Bottom);
        }

        [TestMethod]
        public void SetRightTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            rect.Right = 150;
            Assert.AreEqual(140, rect.Width);
        }

        [TestMethod]
        public void SetBottomTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            rect.Bottom = 300;
            Assert.AreEqual(280, rect.Height);
        }

        [TestMethod]
        public void OffsetPointTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            rect.Offset(new Point(5, 10));
            Assert.AreEqual(15, rect.X);
            Assert.AreEqual(30, rect.Y);
        }

        [TestMethod]
        public void OffsetXYTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            rect.Offset(5, 10);
            Assert.AreEqual(15, rect.X);
            Assert.AreEqual(30, rect.Y);
        }

        [TestMethod]
        public void StaticOffsetPointTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            var result = Rectangle.Offset(rect, new Point(5, 10));
            Assert.AreEqual(15, result.X);
            Assert.AreEqual(30, result.Y);
            Assert.AreEqual(10, rect.X);
        }

        [TestMethod]
        public void StaticOffsetXYTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            var result = Rectangle.Offset(rect, 5, 10);
            Assert.AreEqual(15, result.X);
            Assert.AreEqual(30, result.Y);
        }

        [TestMethod]
        public void ContainsPointTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            Assert.IsTrue(rect.Contains(new Point(50, 100)));
            Assert.IsTrue(rect.Contains(new Point(10, 20)));
            Assert.IsTrue(rect.Contains(new Point(110, 220)));
            Assert.IsFalse(rect.Contains(new Point(5, 100)));
            Assert.IsFalse(rect.Contains(new Point(50, 300)));
        }

        [TestMethod]
        public void ContainsRectangleTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            Assert.IsTrue(rect.Contains(new Rectangle(20, 30, 50, 50)));
            Assert.IsFalse(rect.Contains(new Rectangle(5, 30, 50, 50)));
            Assert.IsFalse(rect.Contains(new Rectangle(20, 30, 200, 50)));
        }

        [TestMethod]
        public void EqualsTest()
        {
            var rect1 = new Rectangle(10, 20, 100, 200);
            var rect2 = new Rectangle(10, 20, 100, 200);
            var rect3 = new Rectangle(10, 20, 100, 201);

            Assert.IsTrue(rect1.Equals(rect2));
            Assert.IsFalse(rect1.Equals(rect3));
            Assert.IsTrue(rect1.Equals((object)rect2));
            Assert.IsFalse(rect1.Equals("not a rectangle"));
        }

        [TestMethod]
        public void OperatorEqualsTest()
        {
            var rect1 = new Rectangle(10, 20, 100, 200);
            var rect2 = new Rectangle(10, 20, 100, 200);
            var rect3 = new Rectangle(10, 20, 100, 201);

            Assert.IsTrue(rect1 == rect2);
            Assert.IsFalse(rect1 == rect3);
            Assert.IsFalse(rect1 != rect2);
            Assert.IsTrue(rect1 != rect3);
        }

        [TestMethod]
        public void ToStringTest()
        {
            var rect = new Rectangle(10, 20, 100, 200);
            Assert.AreEqual("Rect: 10, 20, 100, 200", rect.ToString());
        }

        [TestMethod]
        public void CopyFromTest()
        {
            var source = new Rectangle(10, 20, 100, 200);
            var dest = new Rectangle();
            dest.CopyFrom(source);
            Assert.AreEqual(source.X, dest.X);
            Assert.AreEqual(source.Y, dest.Y);
            Assert.AreEqual(source.Width, dest.Width);
            Assert.AreEqual(source.Height, dest.Height);
        }
    }
}
