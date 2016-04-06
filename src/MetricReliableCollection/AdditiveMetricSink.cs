// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MetricReliableCollections.Extensions;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    internal class AdditiveMetricSink : IMetricSink
    {
        private readonly IReliableStateManager stateManager;
        private readonly string metricStoreName;

        public AdditiveMetricSink(IReliableStateManager stateManager, string metricStoreName)
        {
            this.stateManager = stateManager;
            this.metricStoreName = metricStoreName;
        }

        public async Task ReportLoadAsync(ITransaction tx, Uri collectionName, IEnumerable<LoadMetric> metrics)
        {
            IReliableDictionary<string, List<LoadMetric>> metricDictionary =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<string, List<LoadMetric>>>(this.metricStoreName);

            await metricDictionary.SetAsync(tx, collectionName.ToString(), metrics.ToList());
        }

        public async Task AddLoadAsync(ITransaction tx, Uri collectionName, IEnumerable<LoadMetric> metrics)
        {
            IReliableDictionary<string, List<LoadMetric>> metricDictionary =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<string, List<LoadMetric>>>(this.metricStoreName);

            await metricDictionary.AddOrUpdateAsync(
                tx,
                collectionName.ToString(),
                metrics.ToList(),
                (key, value) =>
                {
                    List<LoadMetric> currentMetrics = new List<LoadMetric>();
                    foreach (LoadMetric newMetric in metrics)
                    {
                        LoadMetric current = value.Find(x => String.Equals(x.Name, newMetric.Name, StringComparison.OrdinalIgnoreCase));

                        if (current == null)
                        {
                            currentMetrics.Add(newMetric);
                        }
                        else
                        {
                            currentMetrics.Add(new LoadMetric(current.Name, current.Value + newMetric.Value));
                        }
                    }

                    return currentMetrics;
                });
        }

        internal async Task<IEnumerable<LoadMetric>> SumMetricsAsync(CancellationToken token)
        {
            IReliableDictionary<string, List<LoadMetric>> metricDictionary =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<string, List<LoadMetric>>>(this.metricStoreName);

            Dictionary<string, int> totals = new Dictionary<string, int>();

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await metricDictionary.ForeachAsync(
                    tx,
                    token,
                    item =>
                    {
                        foreach (LoadMetric metric in item.Value)
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
                    });
            }

            return totals.Select(x => new LoadMetric(x.Key, x.Value));
        }
    }
}