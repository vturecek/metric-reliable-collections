// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests
{
    using System;
    using MetricReliableCollections.ReliableStateSerializers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BinaryValueConverterTest
    {
        [TestMethod]
        public void SerializeDeserializeEmptyArray()
        {
            int[] expected = new int[0];
            BinaryValueConverter target = new BinaryValueConverter(new Uri("store://a"), new JsonReliableStateSerializerResolver());

            BinaryValue result = target.Serialize<int[]>(expected);

            int[] actual = target.Deserialize<int[]>(result);

            Assert.AreEqual(0, actual.Length);
        }

        [TestMethod]
        public void SerializeDeserializeString()
        {
            string expected = "Mr. Burns";
            BinaryValueConverter target = new BinaryValueConverter(new Uri("store://a"), new JsonReliableStateSerializerResolver());

            BinaryValue result = target.Serialize<string>(expected);

            string actual = target.Deserialize<string>(result);

            Assert.AreEqual<string>(expected, actual);
        }
    }
}