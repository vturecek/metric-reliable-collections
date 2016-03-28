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

    internal interface IMetricSink
    {
        /// <summary>
        /// Reports the given load metrics, overriding any previous values of the same name.
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="collectionName"></param>
        /// <param name="metrics"></param>
        /// <returns></returns>
        Task ReportLoadAsync(ITransaction tx, Uri collectionName, IEnumerable<LoadMetric> metrics);

        /// <summary>
        /// Adds the values of the given load metrics to the previous values of the same name,
        /// or adds the values to 0 if metrics with the given names don't exist.
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="collectionName"></param>
        /// <param name="metrics"></param>
        /// <returns></returns>
        Task AddLoadAsync(ITransaction tx, Uri collectionName, IEnumerable<LoadMetric> metrics);
    }
}
