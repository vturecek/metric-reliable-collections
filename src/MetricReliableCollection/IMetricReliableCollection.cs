// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;

    internal interface IMetricReliableCollection
    {
        Task<IEnumerable<LoadMetric>> GetLoadMetricsAsync(ITransaction tx, CancellationToken cancellationToken);
    }
}