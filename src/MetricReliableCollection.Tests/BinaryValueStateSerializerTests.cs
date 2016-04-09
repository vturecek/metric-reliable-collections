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
            BinaryValue expected = new BinaryValue(new byte[0]);
            BinaryValue actual = SerializeDeserialize(expected);

            Assert.AreEqual<BinaryValue>(expected, actual);
        }

        [TestMethod]
        public void SerializeDeserialize8BitLength()
        {
            BinaryValue expected = new BinaryValue(new[] { (byte)5 }, false);

            BinaryValue actual = SerializeDeserialize(expected);

            Assert.AreEqual<BinaryValue>(expected, actual);
        }

        [TestMethod]
        public void SerializeDeserialize16BitLength()
        {
            Random rand = new Random();
            byte[] payload = new byte[65535];
            for (int i = 0; i < 65535; ++i)
            {
                payload[i] = (byte)rand.Next();
            }

            BinaryValue expected = new BinaryValue(payload);

            BinaryValue actual = SerializeDeserialize(expected);

            Assert.AreEqual<BinaryValue>(expected, actual);
        }

        [TestMethod]
        public void SerializeDeserialize24BitLength()
        {
            Random rand = new Random();
            byte[] payload = new byte[16777215];
            for (int i = 0; i < 16777215; ++i)
            {
                payload[i] = (byte)rand.Next();
            }

            BinaryValue expected = new BinaryValue(payload);

            BinaryValue actual = SerializeDeserialize(expected);

            Assert.AreEqual<BinaryValue>(expected, actual);
        }

        [TestMethod]
        public void SerializationSizeByte()
        {
            BinaryValue target = new BinaryValue(new[] { (byte)5 });

            Assert.AreEqual<long>(2, GetSerializdSize(target));
        }

        [TestMethod]
        public void SerializationSize4Bytes()
        {
            BinaryValue target = new BinaryValue(BitConverter.GetBytes(Int32.MaxValue), false);

            Assert.AreEqual<long>(5, GetSerializdSize(target));
        }

        [TestMethod]
        public void SerializationSize127Bytes()
        {
            byte[] payload = new byte[127];
            for (int i = 0; i < 127; ++i)
            {
                payload[i] = (byte)7;
            }

            BinaryValue target = new BinaryValue(payload);

            Assert.AreEqual<long>(128, GetSerializdSize(target));
        }

        [TestMethod]
        public void SerializationSize255Bytes()
        {
            byte[] payload = new byte[255];
            for (int i = 0; i < 255; ++i)
            {
                payload[i] = (byte)7;
            }

            BinaryValue target = new BinaryValue(payload);

            Assert.AreEqual<long>(257, GetSerializdSize(target));
        }

        [TestMethod]
        public void SerializationSize8Bytes()
        {
            BinaryValue target = new BinaryValue(BitConverter.GetBytes(Int64.MaxValue));

            Assert.AreEqual<long>(9, GetSerializdSize(target));
        }

        private long GetSerializdSize(BinaryValue input)
        {
            BinaryValueStateSerializer target = new BinaryValueStateSerializer();

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                target.Write(input, writer);

                return stream.Length;
            }
        }

        private BinaryValue SerializeDeserialize(BinaryValue input)
        {
            BinaryValueStateSerializer target = new BinaryValueStateSerializer();

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                target.Write(input, writer);

                stream.Seek(0, SeekOrigin.Begin);

                BinaryReader reader = new BinaryReader(stream);
                return target.Read(reader);
            }
        }
    }
}