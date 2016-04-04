// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests
{
    using MetricReliableCollections.Tests.Mocks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class AdditiveMetricSinkTests
    {
        [TestMethod]
        public async Task ReportSingleCollectionMultipleMetrics()
        {
            string expectedMetric1 = "one";
            string expectedMetric2 = "two";

            int expectedMetric1Value = 1;
            int expectedMetric2Value = 2;

            Uri collectionName = new Uri("test://dictionary");
            MockReliableStateManager stateManager = new MockReliableStateManager();
            AdditiveMetricSink target = new AdditiveMetricSink(stateManager, "test");

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.ReportLoadAsync(tx, collectionName, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value),
                    new LoadMetric(expectedMetric2, expectedMetric2Value)
                });

                await tx.CommitAsync();
            }

            IEnumerable<LoadMetric> result = await target.SumMetricsAsync(CancellationToken.None);

            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric1 && x.Value == expectedMetric1Value));
            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric2 && x.Value == expectedMetric2Value));
        }

        [TestMethod]
        public async Task ReportMultipleCollectionsSameMetrics()
        {
            string expectedMetric1 = "one";
            string expectedMetric2 = "two";

            int expectedMetric1Value = 2;
            int expectedMetric2Value = 4;

            Uri collection1Name = new Uri("test://dictionary1");
            Uri collection2Name = new Uri("test://dictionary2");

            MockReliableStateManager stateManager = new MockReliableStateManager();
            AdditiveMetricSink target = new AdditiveMetricSink(stateManager, "test");

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.ReportLoadAsync(tx, collection1Name, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value / 2),
                    new LoadMetric(expectedMetric2, expectedMetric2Value / 2)
                });

                await tx.CommitAsync();
            }

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.ReportLoadAsync(tx, collection2Name, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value / 2),
                    new LoadMetric(expectedMetric2, expectedMetric2Value / 2)
                });

                await tx.CommitAsync();
            }

            IEnumerable<LoadMetric> result = await target.SumMetricsAsync(CancellationToken.None);

            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric1 && x.Value == expectedMetric1Value));
            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric2 && x.Value == expectedMetric2Value));
        }

        [TestMethod]
        public async Task ReportMultipleCollectionsDifferentMetrics()
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
            AdditiveMetricSink target = new AdditiveMetricSink(stateManager, "test");

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.ReportLoadAsync(tx, collection1Name, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value),
                    new LoadMetric(expectedMetric2, expectedMetric2Value)
                });

                await tx.CommitAsync();
            }
            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.ReportLoadAsync(tx, collection2Name, new[]
                {
                    new LoadMetric(expectedMetric3, expectedMetric3Value)
                });

                await tx.CommitAsync();
            }

            IEnumerable<LoadMetric> result = await target.SumMetricsAsync(CancellationToken.None);

            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric1 && x.Value == expectedMetric1Value));
            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric2 && x.Value == expectedMetric2Value));
            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric3 && x.Value == expectedMetric3Value));
        }


        [TestMethod]
        public async Task AddSingleCollectionSingleMetric()
        {
            string expectedMetric1 = "one";

            int expectedMetricValue1 = 1;
            int expectedMetricValue2 = 2;

            Uri collectionName = new Uri("test://dictionary");
            MockReliableStateManager stateManager = new MockReliableStateManager();
            AdditiveMetricSink target = new AdditiveMetricSink(stateManager, "test");

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.AddLoadAsync(tx, collectionName, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetricValue1)
                });

                await tx.CommitAsync();
            }

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.AddLoadAsync(tx, collectionName, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetricValue2)
                });

                await tx.CommitAsync();
            }

            IEnumerable<LoadMetric> result = await target.SumMetricsAsync(CancellationToken.None);

            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric1 && x.Value == expectedMetricValue1 + expectedMetricValue2));
        }

        [TestMethod]
        public async Task AddSingleCollectionMultipleMetrics()
        {
            string expectedMetric1 = "one";
            string expectedMetric2 = "two";

            int expectedMetric1Value1 = 1;
            int expectedMetric1Value2 = 2;

            int expectedMetric2Value1 = 3;
            int expectedMetric2Value2 = 4;

            Uri collectionName = new Uri("test://dictionary");
            MockReliableStateManager stateManager = new MockReliableStateManager();
            AdditiveMetricSink target = new AdditiveMetricSink(stateManager, "test");

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.AddLoadAsync(tx, collectionName, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value1),
                    new LoadMetric(expectedMetric2, expectedMetric2Value1)
                });

                await tx.CommitAsync();
            }

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.AddLoadAsync(tx, collectionName, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value2),
                    new LoadMetric(expectedMetric2, expectedMetric2Value2)
                });

                await tx.CommitAsync();
            }

            IEnumerable<LoadMetric> result = await target.SumMetricsAsync(CancellationToken.None);

            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric1 && x.Value == expectedMetric1Value1 + expectedMetric1Value2));
            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric2 && x.Value == expectedMetric2Value1 + expectedMetric2Value2));
        }

        [TestMethod]
        public async Task AddMultipleCollectionsSameMetrics()
        {
            string expectedMetric1 = "one";
            string expectedMetric2 = "two";

            int expectedMetric1Value1 = 2;
            int expectedMetric1Value2 = 6;

            int expectedMetric2Value1 = 10;
            int expectedMetric2Value2 = 12;

            Uri collection1Name = new Uri("test://dictionary1");
            Uri collection2Name = new Uri("test://dictionary2");

            MockReliableStateManager stateManager = new MockReliableStateManager();
            AdditiveMetricSink target = new AdditiveMetricSink(stateManager, "test");

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.AddLoadAsync(tx, collection1Name, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value1 / 2),
                    new LoadMetric(expectedMetric2, expectedMetric2Value1 / 2)
                });

                await target.AddLoadAsync(tx, collection1Name, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value2 / 2),
                    new LoadMetric(expectedMetric2, expectedMetric2Value2 / 2)
                });

                await tx.CommitAsync();
            }

            using (ITransaction tx = stateManager.CreateTransaction())
            {
                await target.AddLoadAsync(tx, collection2Name, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value1 / 2),
                    new LoadMetric(expectedMetric2, expectedMetric2Value1 / 2)
                });

                await target.AddLoadAsync(tx, collection2Name, new[]
                {
                    new LoadMetric(expectedMetric1, expectedMetric1Value2 / 2),
                    new LoadMetric(expectedMetric2, expectedMetric2Value2 / 2)
                });

                await tx.CommitAsync();
            }

            IEnumerable<LoadMetric> result = await target.SumMetricsAsync(CancellationToken.None);

            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric1 && x.Value == expectedMetric1Value1 + expectedMetric1Value2));
            Assert.AreEqual<int>(1, result.Count(x => x.Name == expectedMetric2 && x.Value == expectedMetric2Value1 + expectedMetric2Value2));
        }
    }
}
