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
        public Task GetLoadMetricsEmptyDictionary()
        {
            return RunDataSizeUnitsPermutationAsync(async config =>
            {
                int expected = 0;

                Uri collectionName = new Uri("test://dictionary");
                MockReliableDictionary<BinaryValue, BinaryValue> store = new MockReliableDictionary<BinaryValue, BinaryValue>(collectionName);

                BinaryValueConverter converter = new BinaryValueConverter(collectionName, new JsonReliableStateSerializerResolver());

                MetricReliableDictionary<int, string> target = new MetricReliableDictionary<int, string>(store, converter, config);

                using (ITransaction tx = new MockTransaction())
                {
                    IEnumerable<DecimalLoadMetric> result = await target.GetLoadMetricsAsync(tx, CancellationToken.None);

                    Assert.AreEqual<int>(1, result.Count(x => x.Name == config.MemoryMetricName && x.Value == expected));
                    Assert.AreEqual<int>(1, result.Count(x => x.Name == config.DiskMetricName && x.Value == expected));
                }
            });
        }

        [TestMethod]
        public Task GetLoadMetricsSingleItem()
        {
            return this.RunDataSizeUnitsPermutationAsync(async config =>
            {
                Uri collectionName = new Uri("test://dictionary");

                int key = 1;
                string value = "Homer";

                BinaryValueConverter converter = new BinaryValueConverter(collectionName, new JsonReliableStateSerializerResolver());

                double size = 
                    converter.Serialize<int>(key).Buffer.Length + converter.Serialize<string>(value).Buffer.Length;

                double expectedMemory = size / (double)config.MemoryMetricUnits;
                double expectedDisk = size / (double)config.DiskMetricUnits;

                MockReliableDictionary<BinaryValue, BinaryValue> store = new MockReliableDictionary<BinaryValue, BinaryValue>(collectionName);
                MetricReliableDictionary<int, string> target = new MetricReliableDictionary<int, string>(store, converter, config);

                using (ITransaction tx = new MockTransaction())
                {
                    await target.SetAsync(tx, key, value);
                    await tx.CommitAsync();
                }

                using (ITransaction tx = new MockTransaction())
                {
                    IEnumerable<DecimalLoadMetric> result = await target.GetLoadMetricsAsync(tx, CancellationToken.None);

                    Assert.AreEqual<int>(1, result.Count(x => x.Name == config.MemoryMetricName && x.Value == expectedMemory));
                    Assert.AreEqual<int>(1, result.Count(x => x.Name == config.DiskMetricName && x.Value == expectedDisk));
                }
            });
        }

        [TestMethod]
        public  Task GetLoadMetricsMultipleItem()
        {
            return this.RunDataSizeUnitsPermutationAsync(async config =>
            {
                int key1 = 1;
                string value1 = "Homer";

                int key2 = 2;
                string value2 = "Simpson";

                Uri collectionName = new Uri("test://dictionary");
                
                BinaryValueConverter converter = new BinaryValueConverter(collectionName, new JsonReliableStateSerializerResolver());

                double size =
                        converter.Serialize<int>(key1).Buffer.Length + converter.Serialize<string>(value1).Buffer.Length +
                        converter.Serialize<int>(key2).Buffer.Length + converter.Serialize<string>(value2).Buffer.Length;

                double expectedMemory = size / (double)config.MemoryMetricUnits;
                double expectedDisk = size / (double)config.DiskMetricUnits;

                MockReliableDictionary<BinaryValue, BinaryValue> store = new MockReliableDictionary<BinaryValue, BinaryValue>(collectionName);
                MetricReliableDictionary<int, string> target = new MetricReliableDictionary<int, string>(store, converter, config);

                using (ITransaction tx = new MockTransaction())
                {
                    await target.SetAsync(tx, key1, value1);
                    await target.SetAsync(tx, key2, value2);
                    await tx.CommitAsync();
                }
                using (ITransaction tx = new MockTransaction())
                {
                    IEnumerable<DecimalLoadMetric> result = await target.GetLoadMetricsAsync(tx, CancellationToken.None);
                    
                    Assert.AreEqual<int>(1, result.Count(x => x.Name == config.MemoryMetricName && x.Value == expectedMemory));
                    Assert.AreEqual<int>(1, result.Count(x => x.Name == config.DiskMetricName && x.Value == expectedDisk));
                }
            });
        }

        private async Task RunDataSizeUnitsPermutationAsync(Func<MetricConfiguration, Task> test)
        {
            foreach (DataSizeUnits memory in Enum.GetValues(typeof(DataSizeUnits)))
            {
                foreach (DataSizeUnits disk in Enum.GetValues(typeof(DataSizeUnits)))
                {
                    await test(this.GetConfig(memory, disk));
                }
            }
        }

        private MetricConfiguration GetConfig(DataSizeUnits memoryUnits, DataSizeUnits diskUnits)
        {
            return new MetricConfiguration(
                "MemoryKB",
                memoryUnits,
                "DiskKB",
                diskUnits,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(4));
        }
    }
}