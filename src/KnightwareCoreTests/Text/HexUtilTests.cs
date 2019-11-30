using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Knightware.Text
{
    [TestClass]
    public class HexUtilTests
    {
        [TestMethod]
        public void ToStringFromStringTest()
        {
            string expected = "33343536373839404142434445464748";
            if (!HexUtil.IsValidHexCharLength(expected))
                expected += "A";

            byte[] bytes = HexUtil.GetBytes(expected);
            string actual = HexUtil.GetString(bytes);

            Assert.AreEqual(expected, actual, "Failed to convert string to and from byte array");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ToBytesFromInvalidStringTest()
        {
            //Ensure we start with an invalid test string
            string testString = "AAA";
            if (HexUtil.IsValidHexCharLength(testString))
                Assert.Inconclusive("Failed to start with a known bad test string");

            //Expect an exception here since we have an invalid length
            HexUtil.GetBytes(testString);
        }

        [TestMethod]
        public void IsValidHexLength()
        {
            string invalid = "A";
            Assert.IsFalse(HexUtil.IsValidHexCharLength(invalid), "Should have returned false");

            string valid = "AA";
            Assert.IsTrue(HexUtil.IsValidHexCharLength(valid), "Should have returned true");
        }
    }
}
