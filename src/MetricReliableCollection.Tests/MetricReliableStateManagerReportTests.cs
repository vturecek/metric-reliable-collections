// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests
{
    using System;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using MetricReliableCollections.ReliableStateSerializers;
    using MetricReliableCollections.Tests.Mocks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MetricReliableStateManagerReportTests
    {
        [TestMethod]
        public async Task ReportLoadOnPrimary()
        {
            bool actual = await this.ReportLoadOnChangeRole(ReplicaRole.Primary);

            Assert.IsTrue(actual);
        }

        [TestMethod]
        public async Task ReportLoadOnActiveSecondary()
        {
            bool actual = await this.ReportLoadOnChangeRole(ReplicaRole.ActiveSecondary);

            Assert.IsTrue(actual);
        }

        private async Task<bool> ReportLoadOnChangeRole(ReplicaRole role)
        {
            MetricReliableStateManager target = new MetricReliableStateManager(
                this.GetContext(),
                new JsonReliableStateSerializerResolver(),
                new MockReliableStateManager());

            ManualResetEvent reset = new ManualResetEvent(false);

            bool actual = false;

            MockStatefulServicePartition partition = new MockStatefulServicePartition()
            {
                OnReportLoad = (metrics) =>
                {
                    actual = true;
                    reset.Set();
                }
            };

            await target.OpenAsync(ReplicaOpenMode.New, partition, CancellationToken.None);
            await target.ChangeRoleAsync(role, CancellationToken.None);

            // this may yield false negatives because we're at the mercy of the task scheduler
            // to actually execute the reporting task in a timely manner, which depends on external factors.
            reset.WaitOne(TimeSpan.FromSeconds(30));

            return actual;
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