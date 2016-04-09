// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BinaryValueTests
    {
        [TestMethod]
        public void CreateEmptyBuffer()
        {
            BinaryValue target = new BinaryValue(new byte[0]);

            Assert.AreEqual<int>(0, target.Buffer.Length);
        }

        [TestMethod]
        public void CreateEmptyBufferNoCopy()
        {
            BinaryValue target = new BinaryValue(new byte[0], false);

            Assert.AreEqual<int>(0, target.Buffer.Length);
        }
        
        [TestMethod]
        public void EmptyBufferHashcode()
        {
            BinaryValue target = new BinaryValue(new byte[0]);

            Assert.AreEqual<int>(0, target.GetHashCode());
        }

        [TestMethod]
        public void TestComparable()
        {
            BinaryValue less = new BinaryValue(BitConverter.GetBytes(1));

            BinaryValue same = new BinaryValue(BitConverter.GetBytes(1));

            BinaryValue greater = new BinaryValue(BitConverter.GetBytes(2));

            Assert.AreEqual<int>(less.GetHashCode(), same.GetHashCode());
            Assert.AreEqual<int>(-1, less.CompareTo(greater));
            Assert.AreEqual<int>(0, less.CompareTo(same));
            Assert.AreEqual<int>(1, greater.CompareTo(less));
        }

        [TestMethod]
        public void TestComparableDefault()
        {
            BinaryValue defaultValue = default(BinaryValue);

            BinaryValue defaultValueSame = default(BinaryValue);

            BinaryValue greater = new BinaryValue(BitConverter.GetBytes(2));

            Assert.AreEqual<int>(defaultValue.GetHashCode(), defaultValueSame.GetHashCode());
            Assert.AreEqual<int>(-1, defaultValue.CompareTo(greater));
            Assert.AreEqual<int>(0, defaultValue.CompareTo(defaultValueSame));
            Assert.AreEqual<int>(1, greater.CompareTo(defaultValue));
        }

        [TestMethod]
        public void TestEquatable()
        {
            BinaryValue less = new BinaryValue(BitConverter.GetBytes(1));

            BinaryValue same = new BinaryValue(BitConverter.GetBytes(1));

            BinaryValue greater = new BinaryValue(BitConverter.GetBytes(2));

            Assert.AreEqual<int>(less.GetHashCode(), same.GetHashCode());

            Assert.IsFalse(less.Equals(greater));
            Assert.IsTrue(less.Equals(same));
            Assert.IsFalse(greater.Equals(less));

            Assert.IsFalse(less.Equals((object) greater));
            Assert.IsTrue(less.Equals((object) same));
            Assert.IsFalse(greater.Equals((object) less));
        }

        [TestMethod]
        public void TestEquatableDefault()
        {
            BinaryValue defaultValue = default(BinaryValue);

            BinaryValue defaultValueSame = default(BinaryValue);

            BinaryValue greater = new BinaryValue(BitConverter.GetBytes(2));

            Assert.AreEqual<int>(defaultValue.GetHashCode(), defaultValueSame.GetHashCode());

            Assert.IsFalse(defaultValue.Equals(greater));
            Assert.IsFalse(greater.Equals(defaultValue));
            Assert.IsTrue(defaultValue.Equals(defaultValueSame));

            Assert.IsFalse(defaultValue.Equals((object) greater));
            Assert.IsFalse(greater.Equals((object) defaultValue));
            Assert.IsTrue(defaultValue.Equals((object) defaultValueSame));
        }

        [TestMethod]
        public void TestHashcode()
        {
            string[] testStrings = {"", "A", "One", "D'oh", "Duff", "pneumonoultramicroscopicsilicovolcanoconiosis"};

            foreach (string test in testStrings)
            {
                BinaryValue target = new BinaryValue(Encoding.UTF8.GetBytes(test));

                BinaryValue same = new BinaryValue(Encoding.UTF8.GetBytes(test));

                BinaryValue different = new BinaryValue(Encoding.UTF8.GetBytes(test + "different"));

                Assert.AreEqual<int>(target.GetHashCode(), same.GetHashCode());
                Assert.IsTrue(target.Equals(same));

                if (target.GetHashCode() != different.GetHashCode())
                {
                    Assert.IsFalse(target.Equals(different));
                }
            }
        }

        [TestMethod]
        public void TestHashCodeInDictionary()
        {
            Dictionary<BinaryValue, int> dictionary = new Dictionary<BinaryValue, int>();

            for (int i = 0; i < Int32.MaxValue && i > 0; i += Int32.MaxValue/100)
            {
                BinaryValue target = new BinaryValue(BitConverter.GetBytes(i));

                dictionary[target] = i;
            }

            foreach (KeyValuePair<BinaryValue, int> item in dictionary)
            {
                Assert.AreEqual<int>(BitConverter.ToInt32(item.Key.Buffer, 0), item.Value);
            }
        }
    }
}