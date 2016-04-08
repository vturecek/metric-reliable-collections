// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MetricReliableCollections.Extensions;
    using Microsoft.ServiceFabric.Data;
    using System;
    /// <summary>
    /// Aggregates load metrics for all IMetricReliableCollection in a state manager.
    /// </summary>
    internal class MetricAggregator
    {
        public async Task<IEnumerable<LoadMetric>> Aggregate(IReliableStateManager stateManager, CancellationToken cancellationToken)
        {
            Dictionary<string, double> totals = new Dictionary<string, double>();

            await stateManager.ForeachAsync(
                cancellationToken,
                async item =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IMetricReliableCollection metricCollection = item as IMetricReliableCollection;

                    if (metricCollection != null)
                    {
                        using (ITransaction tx = stateManager.CreateTransaction())
                        {
                            IEnumerable<DecimalLoadMetric> metrics = await metricCollection.GetLoadMetricsAsync(tx, cancellationToken);

                            foreach (DecimalLoadMetric metric in metrics)
                            {
                                if (totals.ContainsKey(metric.Name))
                                {
                                    totals[metric.Name] += metric.Value;
                                }
                                else
                                {
                                    totals[metric.Name] = metric.Value;
                                }
                            }
                        }
                    }
                });

            return totals.Select(x => new LoadMetric(x.Key, (int)Math.Round(x.Value)));
        }
    }
}