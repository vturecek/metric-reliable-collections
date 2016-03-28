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
    using MetricReliableCollections.Extensions;
    using MetricReliableCollections.ReliableStateSerializers;
    using MetricReliableCollections.Tests.Mocks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MetricReliableStateManagerTests
    {
        [TestMethod]
        public async Task GetOrAddAsyncResultType()
        {
            MetricReliableStateManager target = new MetricReliableStateManager(
                this.GetContext(),
                new JsonReliableStateSerializerResolver(),
                new MockReliableStateManager());

            IReliableDictionary<int, string> actual = await target.GetOrAddAsync<IReliableDictionary<int, string>>("test://dictionary");

            Assert.IsTrue(actual is MetricReliableDictionary<int, string>);
        }

        [TestMethod]
        public async Task GetOrAddAsyncTestResultGetSet()
        {
            MetricReliableStateManager target = new MetricReliableStateManager(
                this.GetContext(),
                new JsonReliableStateSerializerResolver(),
                new MockReliableStateManager());

            IReliableDictionary<int, string> dictionary = await target.GetOrAddAsync<IReliableDictionary<int, string>>("test://dictionary");

            string expected = "testvalue";

            using (ITransaction tx = target.CreateTransaction())
            {
                await dictionary.SetAsync(tx, 1, expected);
                await tx.CommitAsync();
            }

            using (ITransaction tx = target.CreateTransaction())
            {
                ConditionalValue<string> actual = await dictionary.TryGetValueAsync(tx, 1);

                Assert.AreEqual<string>(expected, actual.Value);
            }
        }

        [TestMethod]
        public async Task GetAsyncNoResult()
        {
            MetricReliableStateManager target = new MetricReliableStateManager(
                this.GetContext(),
                new JsonReliableStateSerializerResolver(),
                new MockReliableStateManager());

            ConditionalValue<IReliableDictionary<int, string>> actual = await target.TryGetAsync<IReliableDictionary<int, string>>("test://dictionary");

            Assert.IsFalse(actual.HasValue);
            Assert.IsNull(actual.Value);
        }

        [TestMethod]
        public async Task GetAsyncWithResultType()
        {
            Uri name = new Uri("test://dictionary");
            MetricReliableStateManager target = new MetricReliableStateManager(
                this.GetContext(),
                new JsonReliableStateSerializerResolver(),
                new MockReliableStateManager());

            await target.GetOrAddAsync<IReliableDictionary<int, string>>(name);

            ConditionalValue<IReliableDictionary<int, string>> actual = await target.TryGetAsync<IReliableDictionary<int, string>>(name);

            Assert.IsTrue(actual.HasValue);
            Assert.IsTrue(actual.Value is MetricReliableDictionary<int, string>);
        }

        [TestMethod]
        public async Task EnumerateEmpty()
        {
            MetricReliableStateManager target = new MetricReliableStateManager(
                this.GetContext(),
                new JsonReliableStateSerializerResolver(),
                new MockReliableStateManager());
            
            IAsyncEnumerator<IReliableState> enumerator = target.GetAsyncEnumerator();

            bool next = await enumerator.MoveNextAsync(CancellationToken.None);
            
            Assert.IsFalse(next);
            Assert.IsNull(enumerator.Current);
        }

        [TestMethod]
        public async Task EnumerateSingleItem()
        {
            Uri name = new Uri("test://dictionary");
            MetricReliableStateManager target = new MetricReliableStateManager(
                this.GetContext(),
                new JsonReliableStateSerializerResolver(),
                new MockReliableStateManager());

            await target.GetOrAddAsync<IReliableDictionary<int, string>>(name);

            IAsyncEnumerator<IReliableState> enumerator = target.GetAsyncEnumerator();

            bool next = await enumerator.MoveNextAsync(CancellationToken.None);

            MetricReliableDictionary<int, string> actual = enumerator.Current as MetricReliableDictionary<int, string>;

            Assert.IsNotNull(actual);
            Assert.AreEqual<Uri>(name, actual.Name);
            Assert.IsFalse(await enumerator.MoveNextAsync(CancellationToken.None));
        }

        [TestMethod]
        public async Task EnumerateWithNonMetricCollections()
        {
            Uri dictionaryName = new Uri("test://dictionary");
            Uri queueName = new Uri("test://queue");

            MetricReliableStateManager target = new MetricReliableStateManager(
                this.GetContext(),
                new JsonReliableStateSerializerResolver(),
                new MockReliableStateManager());

            await target.GetOrAddAsync<IReliableDictionary<int, string>>(dictionaryName);
            await target.GetOrAddAsync<IReliableQueue<string>>(queueName);

            List<IReliableState> result = new List<IReliableState>();
            await target.ForeachAsync<IReliableState>(CancellationToken.None, x => result.Add(x));

            Assert.AreEqual<int>(2, result.Count);
            Assert.AreEqual<int>(1, result.Count(x => x.Name == queueName));
            Assert.AreEqual<int>(1, result.Count(x => x.Name == dictionaryName && x is MetricReliableDictionary<int, string>));
        }

        private StatefulServiceContext GetContext()
        {
            return new StatefulServiceContext(
                new NodeContext("", new NodeId(0, 0), 0, "", ""),
                new MockCodePackageActivationContext(),
                "",
                new Uri("fabric:/Mock"),
                null,
                Guid.NewGuid(),
                0);
        }
    }
}