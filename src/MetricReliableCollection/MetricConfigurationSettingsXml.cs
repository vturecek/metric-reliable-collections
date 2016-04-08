// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System.Fabric;

    /// <summary>
    /// A version of MetricConfiguration that loads settings from a Settings.xml in a config package.
    /// </summary>
    public class MetricConfigurationSettingsXml : MetricConfiguration
    {
        public MetricConfigurationSettingsXml(ServiceContext context, string configPackageName = "Config")
        {
        }
    }
}