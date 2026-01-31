using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knightware.Primitives
{
    [TestClass]
    public class PointTests
    {
        [TestMethod]
        public void ConstructorTest()
        {
            var point = new Point(10, 20);
            Assert.AreEqual(10, point.X);
            Assert.AreEqual(20, point.Y);
        }

        [TestMethod]
        public void EmptyTest()
        {
            var empty = Point.Empty;
            Assert.AreEqual(0, empty.X);
            Assert.AreEqual(0, empty.Y);
        }

        [TestMethod]
        public void EqualsTest()
        {
            var point1 = new Point(10, 20);
            var point2 = new Point(10, 20);
            var point3 = new Point(10, 21);

            Assert.IsTrue(point1.Equals(point2));
            Assert.IsFalse(point1.Equals(point3));
            Assert.IsTrue(point1.Equals((object)point2));
            Assert.IsFalse(point1.Equals("not a point"));
        }

        [TestMethod]
        public void ToStringTest()
        {
            var point = new Point(10, 20);
            Assert.AreEqual("X=10, Y=20", point.ToString());
        }
    }
}
