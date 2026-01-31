using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Knightware.Collections
{
    [TestClass]
    public class GroupingTests
    {
        [TestMethod]
        public void KeyTest()
        {
            var items = new List<string> { "a", "b", "c" };
            var grouping = new Grouping<int, string>(1, items);
            Assert.AreEqual(1, grouping.Key);
        }

        [TestMethod]
        public void EnumeratorTest()
        {
            var items = new List<string> { "a", "b", "c" };
            var grouping = new Grouping<int, string>(1, items);

            var result = grouping.ToList();
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("a", result[0]);
            Assert.AreEqual("b", result[1]);
            Assert.AreEqual("c", result[2]);
        }

        [TestMethod]
        public void NonGenericEnumeratorTest()
        {
            var items = new List<string> { "a", "b", "c" };
            var grouping = new Grouping<int, string>(1, items);

            var enumerable = (System.Collections.IEnumerable)grouping;
            var enumerator = enumerable.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("a", enumerator.Current);
        }

        [TestMethod]
        public void EmptyGroupingTest()
        {
            var items = new List<string>();
            var grouping = new Grouping<int, string>(1, items);

            Assert.AreEqual(0, grouping.Count());
        }

        [TestMethod]
        public void IGroupingInterfaceTest()
        {
            var items = new List<string> { "a", "b" };
            IGrouping<int, string> grouping = new Grouping<int, string>(42, items);

            Assert.AreEqual(42, grouping.Key);
            Assert.AreEqual(2, grouping.Count());
        }
    }
}
