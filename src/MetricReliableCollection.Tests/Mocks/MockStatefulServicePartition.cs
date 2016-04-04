using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Health;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetricReliableCollections.Tests.Mocks
{
    internal class MockStatefulServicePartition : IStatefulServicePartition
    {
        public Action<IEnumerable<LoadMetric>> OnReportLoad { private get; set; }

        public ServicePartitionInformation PartitionInfo
        {
            get; set;
        }

        public PartitionAccessStatus ReadStatus
        {
            get; set;
        }

        public PartitionAccessStatus WriteStatus
        {
            get; set;
        }

        public FabricReplicator CreateReplicator(IStateProvider stateProvider, ReplicatorSettings replicatorSettings)
        {
            throw new NotImplementedException();
        }

        public void ReportFault(FaultType faultType)
        {
            throw new NotImplementedException();
        }

        public void ReportLoad(IEnumerable<LoadMetric> metrics)
        {
            if (this.OnReportLoad != null)
            {
                this.OnReportLoad(metrics);
            }
        }

        public void ReportMoveCost(MoveCost moveCost)
        {
            throw new NotImplementedException();
        }

        public void ReportPartitionHealth(HealthInformation healthInfo)
        {
            throw new NotImplementedException();
        }

        public void ReportReplicaHealth(HealthInformation healthInfo)
        {
            throw new NotImplementedException();
        }
    }
}
