// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests
{
    using System.IO;
    using System.Linq;
    using MetricReliableCollections.ReliableStateSerializers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonReliableStateSerializerTests
    {
        [TestMethod]
        public void SerializeDeserializeEmpty()
        {
            int[] expected = new int[0];

            JsonReliableStateSerializer<int[]> target = new JsonReliableStateSerializer<int[]>();

            using (MemoryStream stream = new MemoryStream())
            {
                target.Serialize(stream, expected);

                stream.Seek(0, SeekOrigin.Begin);

                int[] actual = target.Deserialize(stream);

                Assert.IsTrue(Enumerable.SequenceEqual(expected, actual));
            }
        }

        [TestMethod]
        public void SerializeDeserializeNormal()
        {
            int[] expected = new[] {1, 2, 3, 4, 5};
            JsonReliableStateSerializer<int[]> target = new JsonReliableStateSerializer<int[]>();

            using (MemoryStream stream = new MemoryStream())
            {
                target.Serialize(stream, expected);

                stream.Seek(0, SeekOrigin.Begin);

                int[] actual = target.Deserialize(stream);

                Assert.IsTrue(Enumerable.SequenceEqual(expected, actual));
            }
        }
    }
}