// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface IMetricReliableCollection
    {
        Task<IEnumerable<KeyValuePair<string, int>>> GetMetricsAsync(CancellationToken token);
    }
}