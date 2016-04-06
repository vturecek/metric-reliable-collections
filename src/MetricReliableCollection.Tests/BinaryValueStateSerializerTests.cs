// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BinaryValueStateSerializerTests
    {
        [TestMethod]
        public void SerializeDeserializeEmpty()
        {
            BinaryValue expected = new BinaryValue(new ArraySegment<byte>(new byte[0]));
            BinaryValueStateSerializer target = new BinaryValueStateSerializer();

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                target.Write(expected, writer);

                stream.Seek(0, SeekOrigin.Begin);

                BinaryReader reader = new BinaryReader(stream);
                BinaryValue actual = target.Read(reader);

                Assert.AreEqual<BinaryValue>(expected, actual);
            }
        }

        [TestMethod]
        public void SerializeDeserializeNormal()
        {
            BinaryValue expected = new BinaryValue(
                new ArraySegment<byte>(BitConverter.GetBytes(2)));

            BinaryValueStateSerializer target = new BinaryValueStateSerializer();

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                target.Write(expected, writer);

                stream.Seek(0, SeekOrigin.Begin);

                BinaryReader reader = new BinaryReader(stream);
                BinaryValue actual = target.Read(reader);

                Assert.AreEqual<BinaryValue>(expected, actual);
            }
        }
    }
}