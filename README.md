# metric-reliable-collections
Service Fabric Reliable Collections that report storage load metrics.

## Wat
Metric Reliable Collections are wrappers around Reliable Collections that automatically report load metrics for memory and disk usage. The load reported is exactly the amount of data stored in each replica of a service that uses Metric Reliable Collections. It does not report total memory and disk used by each replica. Both primary and active secondary replicas report load.

## Usage
 1. Simply replace your ReliableStateManager instance with a MetricReliableStateManager when you create your service class. As long as your service works only with the Reliable Collection interfaces and not the concrete classes, no other changes are required to your service code. You must provide a custom serializer. A JSON serializer is provided by default for the lazy.

 ```csharp
    internal static class Program
    {
        private static void Main()
        {
            ServiceRuntime.RegisterServiceAsync(
                "LoadGenServiceType",
                context => new LoadGenService(
                    context,
                    new MetricReliableStateManager(
                        context,
                        new JsonReliableStateSerializerResolver(),
                        new MetricConfiguration("MemoryKB", DataSizeUnits.Kilobytes, "DiskKB", DataSizeUnits.Kilobytes, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)))))
                .GetAwaiter()
                .GetResult();
                
            // Prevents this host process from terminating so services keep running.
            Thread.Sleep(Timeout.Infinite);
        }
    }
 ```

 2. Set up default metric values for your service instances that match the names provided in MetricConfiguration.

 Default Services in Visual Studio:
 ```xml
    <DefaultServices>
      <Service Name="LoadGenService">
         <StatefulService ServiceTypeName="LoadGenServiceType" TargetReplicaSetSize="3" MinReplicaSetSize="2">
            <UniformInt64Partition PartitionCount="10" LowKey="0" HighKey="10" />
            <LoadMetrics>
               <LoadMetric Name="MemoryKB" />
               <LoadMetric Name="DiskKB" />
            </LoadMetrics>
         </StatefulService>
      </Service>
   </DefaultServices>
 ```
 PowerShell:
 ```powershell
 New-ServiceFabricService -ApplicationName "fabric:/LoadMetrics" -ServiceName "fabric:/LoadMetrics/LoadGenService" -ServiceTypeName "LoadGenServiceType" -Stateful -PartitionSchemeUniformInt64 -PartitionCount 10 -LowKey 0 -HighKey 10 -HasPersistedState -TargetReplicaSetSize 3 -MinReplicaSetSize 3 -Metric @("MemoryKB,High,0,1”, "DiskKB,High,0,1”)
 ```

 3. Set up Node capacities for memory and disk with the same names provided in MetricConfiguration and in the service instances;

 Cluster Manifest:
 ```xml
    <NodeTypes>
        <NodeType Name="NodeType0">
            <Endpoints>
                <ClientConnectionEndpoint Port="19000" />
                <LeaseDriverEndpoint Port="19001" />
                <ClusterConnectionEndpoint Port="19002" />
                <HttpGatewayEndpoint Port="19080" Protocol="http" />
                <ServiceConnectionEndpoint Port="19006" />
                <HttpApplicationGatewayEndpoint Port="19081" Protocol="http" />
                <ApplicationEndpoints StartPort="30001" EndPort="31000" />
            </Endpoints>
			<Capacities>
                <Capacity Name="MemoryKB" Value="1048576"/>
                <Capacity Name="DiskKB" Value="104857600"/>
            </Capacities>
        </NodeType>
       ...
    </NodeTypes>
 ```

## How it works
Services that use Reliable Collections program against the Reliable Collection interfaces. Metric Reliable Collections are implementations of those interfaces. For services that are written against the interfaces, no change to service code should be required to use Metric Reliable Collections.

At the heart is MetricReliableStateManager, an implementation of IReliableStateManager. It manages MetricReliableCollections, implementations of IReliableCollections. MetricReliableStateManager is a wrapper around the built-in ReliableStateManager that creates Metric wrappers around Reliable Collections. The Reliable Collections that are created store byte arrays, and the Metric wrappers around them convert your strongly-typed objects to byte arrays. This allows the Metric classes to quickly and accurately compute usage by totalling up the number of bytes stored in the collections without walking object graphs or using serialization tricks to compute object size. This also allows MetricReliableStateManager to define a flexible custom serialization interface.

## Notes
Still a work-in-progress but ready for a test run.

## Learn more!

 - Background reading on cluster capacity: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-cluster-resource-manager-cluster-description/#cluster-capacity

 - Background reading on load metrics: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-cluster-resource-manager-metrics/
