using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knightware.Primitives
{
    [TestClass]
    public class SizeTests
    {
        [TestMethod]
        public void ConstructorTest()
        {
            var size = new Size(100, 200);
            Assert.AreEqual(100, size.Width);
            Assert.AreEqual(200, size.Height);
        }

        [TestMethod]
        public void EmptyTest()
        {
            var empty = Size.Empty;
            Assert.AreEqual(0, empty.Width);
            Assert.AreEqual(0, empty.Height);
        }

        [TestMethod]
        public void EqualsTest()
        {
            var size1 = new Size(100, 200);
            var size2 = new Size(100, 200);
            var size3 = new Size(100, 201);

            Assert.IsTrue(size1.Equals(size2));
            Assert.IsFalse(size1.Equals(size3));
            Assert.IsTrue(size1.Equals((object)size2));
            Assert.IsFalse(size1.Equals("not a size"));
        }

        [TestMethod]
        public void OperatorEqualsTest()
        {
            var size1 = new Size(100, 200);
            var size2 = new Size(100, 200);
            var size3 = new Size(100, 201);

            Assert.IsTrue(size1 == size2);
            Assert.IsFalse(size1 == size3);
            Assert.IsFalse(size1 != size2);
            Assert.IsTrue(size1 != size3);
        }

        [TestMethod]
        public void ToStringTest()
        {
            var size = new Size(100, 200);
            Assert.AreEqual("Width=100, Height=200", size.ToString());
        }
    }
}
