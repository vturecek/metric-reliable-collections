// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Mocks;
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class MetricAggregatorTests
    {
        [TestMethod]
        public async Task AggregateEmptyStateManager()
        {
            MockReliableStateManager stateManager = new MockReliableStateManager();
            MetricAggregator target = new MetricAggregator();

            IEnumerable<LoadMetric> actual = await target.Aggregate(stateManager, CancellationToken.None);

            Assert.IsFalse(actual.Any());
        }

        [TestMethod]
        public async Task AggregateEmptyMetrics()
        {
            Uri name = new Uri("test://dictionary");
            MockReliableStateManager stateManager = new MockReliableStateManager();
            MockReliableDictionary<int, int> dictionary = new MockReliableDictionary<int, int>(name);
            dictionary.OnGetLoadMetrics = () =>
                new LoadMetric[0];

            stateManager.SetMock(name, dictionary);

            MetricAggregator target = new MetricAggregator();
            IEnumerable<LoadMetric> actual = await target.Aggregate(stateManager, CancellationToken.None);

            Assert.IsFalse(actual.Any());
        }

        [TestMethod]
        public async Task AggregateMultipleMetrics()
        {
            string expectedMetric1 = "one";
            string expectedMetric2 = "two";
            int expectedMetric1Value = 1;
            int expectedMetric2Value = 2;

            Uri name = new Uri("test://dictionary");
            MockReliableStateManager stateManager = new MockReliableStateManager();
            MockReliableDictionary<int, int> dictionary = new MockReliableDictionary<int, int>(name);

            dictionary.OnGetLoadMetrics = () =>
                new LoadMetric[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value),
                    new LoadMetric(expectedMetric2, expectedMetric2Value)
                };

            stateManager.SetMock(name, dictionary);

            MetricAggregator target = new MetricAggregator();
            IEnumerable<LoadMetric> actual = await target.Aggregate(stateManager, CancellationToken.None);
            
            Assert.AreEqual<int>(1, actual.Count(x => x.Name == expectedMetric1 && x.Value == expectedMetric1Value));
            Assert.AreEqual<int>(1, actual.Count(x => x.Name == expectedMetric2 && x.Value == expectedMetric2Value));
        }

        [TestMethod]
        public async Task AggregateMultipleCollectionsSameMetrics()
        {
            string expectedMetric1 = "one";
            string expectedMetric2 = "two";

            int expectedMetric1Value = 2;
            int expectedMetric2Value = 4;

            Uri collection1Name = new Uri("test://dictionary1");
            Uri collection2Name = new Uri("test://dictionary2");

            MockReliableStateManager stateManager = new MockReliableStateManager();
            MockReliableDictionary<int, int> dictionary1 = new MockReliableDictionary<int, int>(collection1Name);
            MockReliableDictionary<int, int> dictionary2 = new MockReliableDictionary<int, int>(collection2Name);

            dictionary1.OnGetLoadMetrics = () =>
                new LoadMetric[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value / 2),
                    new LoadMetric(expectedMetric2, expectedMetric2Value / 2)
                };

            dictionary2.OnGetLoadMetrics = () =>
                new LoadMetric[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value / 2),
                    new LoadMetric(expectedMetric2, expectedMetric2Value / 2)
                };

            stateManager.SetMock(collection1Name, dictionary1);
            stateManager.SetMock(collection2Name, dictionary2);


            MetricAggregator target = new MetricAggregator();
            IEnumerable<LoadMetric> actual = await target.Aggregate(stateManager, CancellationToken.None);

            Assert.AreEqual<int>(1, actual.Count(x => x.Name == expectedMetric1 && x.Value == expectedMetric1Value));
            Assert.AreEqual<int>(1, actual.Count(x => x.Name == expectedMetric2 && x.Value == expectedMetric2Value));
        }

        [TestMethod]
        public async Task AggregateMultipleCollectionsDifferentMetrics()
        {
            string expectedMetric1 = "one";
            string expectedMetric2 = "two";
            string expectedMetric3 = "three";

            int expectedMetric1Value = 1;
            int expectedMetric2Value = 2;
            int expectedMetric3Value = 3;

            Uri collection1Name = new Uri("test://dictionary1");
            Uri collection2Name = new Uri("test://dictionary2");

            MockReliableStateManager stateManager = new MockReliableStateManager();
            MockReliableDictionary<int, int> dictionary1 = new MockReliableDictionary<int, int>(collection1Name);
            MockReliableDictionary<int, int> dictionary2 = new MockReliableDictionary<int, int>(collection2Name);

            dictionary1.OnGetLoadMetrics = () =>
                new LoadMetric[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value),
                    new LoadMetric(expectedMetric2, expectedMetric2Value)
                };
            

            dictionary2.OnGetLoadMetrics = () =>
                new LoadMetric[]
                {
                    new LoadMetric(expectedMetric3, expectedMetric3Value)
                };

            stateManager.SetMock(collection1Name, dictionary1);
            stateManager.SetMock(collection2Name, dictionary2);


            MetricAggregator target = new MetricAggregator();
            IEnumerable<LoadMetric> actual = await target.Aggregate(stateManager, CancellationToken.None);

            Assert.AreEqual<int>(1, actual.Count(x => x.Name == expectedMetric1 && x.Value == expectedMetric1Value));
            Assert.AreEqual<int>(1, actual.Count(x => x.Name == expectedMetric2 && x.Value == expectedMetric2Value));
            Assert.AreEqual<int>(1, actual.Count(x => x.Name == expectedMetric3 && x.Value == expectedMetric3Value));
        }
    }
}