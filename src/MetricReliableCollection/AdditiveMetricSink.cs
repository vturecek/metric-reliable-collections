// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using Microsoft.ServiceFabric.Data;
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading.Tasks;

    internal class AdditiveMetricSink : IMetricSink
    {
        private readonly IReliableStateManager stateManager;
        private readonly string metricStoreName;

        public AdditiveMetricSink(IReliableStateManager stateManager, string metricStoreName)
        {
            this.stateManager = stateManager;
            this.metricStoreName = metricStoreName;
        }

        internal Task<IEnumerable<LoadMetric>> SumMetricsAsync()
        {
            throw new NotImplementedException();
        }
        
        public Task ReportLoadAsync(ITransaction tx, Uri collectionName, IEnumerable<LoadMetric> metrics)
        {
            throw new NotImplementedException();
        }

        public Task AddLoadAsync(ITransaction tx, Uri collectionName, IEnumerable<LoadMetric> metrics)
        {
            throw new NotImplementedException();
        }
    }
}
