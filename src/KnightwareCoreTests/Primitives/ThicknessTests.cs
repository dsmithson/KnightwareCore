using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knightware.Primitives
{
    [TestClass]
    public class ThicknessTests
    {
        [TestMethod]
        public void UniformConstructorTest()
        {
            var thickness = new Thickness(10);
            Assert.AreEqual(10, thickness.Left);
            Assert.AreEqual(10, thickness.Top);
            Assert.AreEqual(10, thickness.Right);
            Assert.AreEqual(10, thickness.Bottom);
        }

        [TestMethod]
        public void IndividualConstructorTest()
        {
            var thickness = new Thickness(1, 2, 3, 4);
            Assert.AreEqual(1, thickness.Left);
            Assert.AreEqual(2, thickness.Top);
            Assert.AreEqual(3, thickness.Right);
            Assert.AreEqual(4, thickness.Bottom);
        }

        [TestMethod]
        public void EmptyTest()
        {
            var empty = Thickness.Empty;
            Assert.AreEqual(0, empty.Left);
            Assert.AreEqual(0, empty.Top);
            Assert.AreEqual(0, empty.Right);
            Assert.AreEqual(0, empty.Bottom);
        }

        [TestMethod]
        public void EqualsTest()
        {
            var t1 = new Thickness(1, 2, 3, 4);
            var t2 = new Thickness(1, 2, 3, 4);
            var t3 = new Thickness(1, 2, 3, 5);

            Assert.IsTrue(t1.Equals(t2));
            Assert.IsFalse(t1.Equals(t3));
            Assert.IsTrue(t1.Equals((object)t2));
            Assert.IsFalse(t1.Equals("not a thickness"));
        }

        [TestMethod]
        public void OperatorEqualsTest()
        {
            var t1 = new Thickness(1, 2, 3, 4);
            var t2 = new Thickness(1, 2, 3, 4);
            var t3 = new Thickness(1, 2, 3, 5);

            Assert.IsTrue(t1 == t2);
            Assert.IsFalse(t1 == t3);
            Assert.IsFalse(t1 != t2);
            Assert.IsTrue(t1 != t3);
        }

        [TestMethod]
        public void ToStringTest()
        {
            var thickness = new Thickness(1, 2, 3, 4);
            Assert.AreEqual("Left=1, Top=2, Right=3, Bottom=4", thickness.ToString());
        }
    }
}
