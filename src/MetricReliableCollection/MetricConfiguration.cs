// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;

    public class MetricConfiguration
    {
        public MetricConfiguration(
            string memoryMetricName,
            DataSizeUnits memoryMetricUnits,
            string diskMetricName,
            DataSizeUnits diskMetricUnits,
            TimeSpan reportInterval,
            TimeSpan defaultOperationTimeout)
        {
            this.MemoryMetricName = memoryMetricName;
            this.MemoryMetricUnits = memoryMetricUnits;
            this.DiskMetricName = diskMetricName;
            this.DiskMetricUnits = diskMetricUnits;
            this.ReportInterval = reportInterval;
            this.DefaultOperationTimeout = defaultOperationTimeout;
        }

        protected MetricConfiguration()
        {
        }

        public string MemoryMetricName { get; protected set; }

        public DataSizeUnits MemoryMetricUnits { get; protected set; }

        public string DiskMetricName { get; protected set; }

        public DataSizeUnits DiskMetricUnits { get; protected set; }

        public TimeSpan ReportInterval { get; protected set; }

        public TimeSpan DefaultOperationTimeout { get; protected set; }
    }
}