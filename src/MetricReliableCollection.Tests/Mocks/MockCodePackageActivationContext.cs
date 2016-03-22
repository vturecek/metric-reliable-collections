// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;

    internal class MockCodePackageActivationContext : ICodePackageActivationContext
    {
        public string ApplicationName
        {
            get { throw new NotImplementedException(); }
        }

        public string ApplicationTypeName
        {
            get { throw new NotImplementedException(); }
        }

        public string CodePackageName
        {
            get { throw new NotImplementedException(); }
        }

        public string CodePackageVersion
        {
            get { throw new NotImplementedException(); }
        }

        public string ContextId
        {
            get { throw new NotImplementedException(); }
        }

        public string LogDirectory
        {
            get { throw new NotImplementedException(); }
        }

        public string TempDirectory
        {
            get { throw new NotImplementedException(); }
        }

        public string WorkDirectory
        {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<PackageAddedEventArgs<CodePackage>> CodePackageAddedEvent;

        public event EventHandler<PackageModifiedEventArgs<CodePackage>> CodePackageModifiedEvent;

        public event EventHandler<PackageRemovedEventArgs<CodePackage>> CodePackageRemovedEvent;

        public event EventHandler<PackageAddedEventArgs<ConfigurationPackage>> ConfigurationPackageAddedEvent;

        public event EventHandler<PackageModifiedEventArgs<ConfigurationPackage>> ConfigurationPackageModifiedEvent;

        public event EventHandler<PackageRemovedEventArgs<ConfigurationPackage>> ConfigurationPackageRemovedEvent;

        public event EventHandler<PackageAddedEventArgs<DataPackage>> DataPackageAddedEvent;

        public event EventHandler<PackageModifiedEventArgs<DataPackage>> DataPackageModifiedEvent;

        public event EventHandler<PackageRemovedEventArgs<DataPackage>> DataPackageRemovedEvent;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ApplicationPrincipalsDescription GetApplicationPrincipals()
        {
            throw new NotImplementedException();
        }

        public IList<string> GetCodePackageNames()
        {
            throw new NotImplementedException();
        }

        public CodePackage GetCodePackageObject(string packageName)
        {
            throw new NotImplementedException();
        }

        public IList<string> GetConfigurationPackageNames()
        {
            throw new NotImplementedException();
        }

        public ConfigurationPackage GetConfigurationPackageObject(string packageName)
        {
            throw new NotImplementedException();
        }

        public IList<string> GetDataPackageNames()
        {
            throw new NotImplementedException();
        }

        public DataPackage GetDataPackageObject(string packageName)
        {
            throw new NotImplementedException();
        }

        public EndpointResourceDescription GetEndpoint(string endpointName)
        {
            throw new NotImplementedException();
        }

        public KeyedCollection<string, EndpointResourceDescription> GetEndpoints()
        {
            throw new NotImplementedException();
        }

        public KeyedCollection<string, ServiceGroupTypeDescription> GetServiceGroupTypes()
        {
            throw new NotImplementedException();
        }

        public string GetServiceManifestName()
        {
            throw new NotImplementedException();
        }

        public string GetServiceManifestVersion()
        {
            throw new NotImplementedException();
        }

        public KeyedCollection<string, ServiceTypeDescription> GetServiceTypes()
        {
            throw new NotImplementedException();
        }
    }
}