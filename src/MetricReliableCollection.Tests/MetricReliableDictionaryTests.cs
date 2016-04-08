// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MetricReliableCollections.ReliableStateSerializers;
    using MetricReliableCollections.Tests.Mocks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MetricReliableDictionaryTests
    {
        [TestMethod]
        public async Task GetLoadMetricsEmptyDictionary()
        {
            int expected = 0;

            MetricConfiguration config = this.GetConfig();
            Uri collectionName = new Uri("test://dictionary");
            MockReliableDictionary<BinaryValue, BinaryValue> store = new MockReliableDictionary<BinaryValue, BinaryValue>(collectionName);

            BinaryValueConverter converter = new BinaryValueConverter(collectionName, new JsonReliableStateSerializerResolver());

            MetricReliableDictionary<int, string> target = new MetricReliableDictionary<int, string>(store, converter, config);

            using (ITransaction tx = new MockTransaction())
            {
                IEnumerable<LoadMetric> result = await target.GetLoadMetricsAsync(tx, CancellationToken.None);

                Assert.AreEqual<int>(1, result.Count(x => x.Name == config.MemoryMetricName && x.Value == expected));
                Assert.AreEqual<int>(1, result.Count(x => x.Name == config.DiskMetricName && x.Value == expected));
            }
        }

        [TestMethod]
        public async Task GetLoadMetricsSingleItem()
        {
            int key = 1;
            string value = "Homer";
            MetricConfiguration config = this.GetConfig();
            Uri collectionName = new Uri("test://dictionary");
            MockReliableDictionary<BinaryValue, BinaryValue> store = new MockReliableDictionary<BinaryValue, BinaryValue>(collectionName);

            BinaryValueConverter converter = new BinaryValueConverter(collectionName, new JsonReliableStateSerializerResolver());

            MetricReliableDictionary<int, string> target = new MetricReliableDictionary<int, string>(store, converter, config);

            using (ITransaction tx = new MockTransaction())
            {
                await target.SetAsync(tx, key, value);
                await tx.CommitAsync();
            }

            using (ITransaction tx = new MockTransaction())
            {
                IEnumerable<LoadMetric> result = await target.GetLoadMetricsAsync(tx, CancellationToken.None);

                int expected = converter.Serialize<int>(key).Buffer.Length + converter.Serialize<string>(value).Buffer.Length;

                Assert.AreEqual<int>(1, result.Count(x => x.Name == config.MemoryMetricName && x.Value == expected));
                Assert.AreEqual<int>(1, result.Count(x => x.Name == config.DiskMetricName && x.Value == expected));
            }
        }

        [TestMethod]
        public async Task GetLoadMetricsMultipleItem()
        {
            int key1 = 1;
            string value1 = "Homer";

            int key2 = 2;
            string value2 = "Simpson";

            Uri collectionName = new Uri("test://dictionary");
            MetricConfiguration config = this.GetConfig();
            MockReliableDictionary<BinaryValue, BinaryValue> store = new MockReliableDictionary<BinaryValue, BinaryValue>(collectionName);

            BinaryValueConverter converter = new BinaryValueConverter(collectionName, new JsonReliableStateSerializerResolver());

            MetricReliableDictionary<int, string> target = new MetricReliableDictionary<int, string>(store, converter, config);

            using (ITransaction tx = new MockTransaction())
            {
                await target.SetAsync(tx, key1, value1);
                await target.SetAsync(tx, key2, value2);
                await tx.CommitAsync();
            }
            using (ITransaction tx = new MockTransaction())
            {
                IEnumerable<LoadMetric> result = await target.GetLoadMetricsAsync(tx, CancellationToken.None);

                int expected =
                    converter.Serialize<int>(key1).Buffer.Length + converter.Serialize<string>(value1).Buffer.Length +
                    converter.Serialize<int>(key2).Buffer.Length + converter.Serialize<string>(value2).Buffer.Length;

                Assert.AreEqual<int>(1, result.Count(x => x.Name == config.MemoryMetricName && x.Value == expected));
                Assert.AreEqual<int>(1, result.Count(x => x.Name == config.DiskMetricName && x.Value == expected));
            }
        }

        private MetricConfiguration GetConfig()
        {
            return new MetricConfiguration(
                "MemoryKB",
                DataSizeUnits.Kilobytes,
                "DiskKB",
                DataSizeUnits.Kilobytes,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(4));
        }
    }
}