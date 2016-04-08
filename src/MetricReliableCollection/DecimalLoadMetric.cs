// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    /// <summary>
    /// Load metric container that allows decimal values.
    /// </summary>
    public struct DecimalLoadMetric
    {
        public DecimalLoadMetric(string name, double Value)
        {
            this.Name = name;
            this.Value = Value;
        }

        public string Name { get; }

        public double Value { get; }
    }
}
