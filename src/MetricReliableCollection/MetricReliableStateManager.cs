// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Data.Notifications;

    /// <summary>
    /// A Reliable State Manager that automatically reports load metrics
    /// on Reliable Collections and adds better support for custom serialization.
    /// </summary>
    public class MetricReliableStateManager : IReliableStateManagerReplica
    {
        /// <summary>
        /// collections that are used for metadata use this URI scheme for their name.
        /// </summary>
        private const string MetricMetadataStoreScheme = "metricreliablestatemetadata";

        /// <summary>
        /// Name of the dictionary that maintains metric collection type information.
        /// </summary>
        private const string MetricCollectionTypeDictionaryName = MetricMetadataStoreScheme + "://metriccollectiontypes";

        private readonly ConcurrentDictionary<Uri, object> collectionCache = new ConcurrentDictionary<Uri, object>();

        private readonly IReliableStateManagerReplica stateManagerReplica;

        private readonly IReliableStateSerializerResolver serializerResolver;

        private readonly MetricConfiguration config;

        private IReliableDictionary<string, string> collectionTypeNamesInstance;

        private IStatefulServicePartition partition;

        private CancellationTokenSource reportTaskCancellation;

        private Task reportTask;

        /// <summary>
        /// Creates a new instance of MetricReliableStateManager.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="serializerResolver"></param>
        /// <param name="configProvider"></param>
        /// <param name="stateManager">
        /// Best to leave this null and let the constructor create one that's configured with a fast binary serializer.
        /// Beware: if you pass in your own instance of ReliableStateManager, it will use DataContract.
        /// </param>
        public MetricReliableStateManager(
            StatefulServiceContext context, IReliableStateSerializerResolver serializerResolver, MetricConfiguration config = null,
            IReliableStateManagerReplica stateManager = null)
        {
            this.serializerResolver = serializerResolver;
            this.config = config ?? new MetricConfigurationSettingsXml(context);
            this.stateManagerReplica =
                stateManager ??
                new ReliableStateManager(
                    context,
                    new ReliableStateManagerConfiguration(
                        onInitializeStateSerializersEvent: () =>
                        {
                            this.stateManagerReplica.StateManagerChanged += this.StateManagerReplica_StateManagerChanged;
                            this.stateManagerReplica.TransactionChanged += this.StateManagerReplica_TransactionChanged;

                            return Task.FromResult(this.stateManagerReplica.TryAddStateSerializer<BinaryValue>(new BinaryValueStateSerializer()));
                        }));
        }

        public event EventHandler<NotifyStateManagerChangedEventArgs> StateManagerChanged;

        public event EventHandler<NotifyTransactionChangedEventArgs> TransactionChanged;

        public Func<CancellationToken, Task<bool>> OnDataLossAsync
        {
            set { this.stateManagerReplica.OnDataLossAsync = value; }
        }

        public ITransaction CreateTransaction()
        {
            return this.stateManagerReplica.CreateTransaction();
        }

        public IAsyncEnumerator<IReliableState> GetAsyncEnumerator()
        {
            return new MetricReliableStateManagerAsyncEnumerator(this);
        }

        public Task<T> GetOrAddAsync<T>(string name) where T : IReliableState
        {
            return this.GetOrAddAsync<T>(new Uri(name));
        }

        public Task<T> GetOrAddAsync<T>(Uri name) where T : IReliableState
        {
            return this.GetOrAddAsync<T>(name, this.config.DefaultOperationTimeout);
        }

        public Task<T> GetOrAddAsync<T>(string name, TimeSpan timeout) where T : IReliableState
        {
            return this.GetOrAddAsync<T>(new Uri(name), timeout);
        }

        public async Task<T> GetOrAddAsync<T>(Uri name, TimeSpan timeout) where T : IReliableState
        {
            using (ITransaction tx = this.CreateTransaction())
            {
                T result = await this.GetOrAddAsync<T>(tx, name, timeout);

                await tx.CommitAsync();

                return result;
            }
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, string name) where T : IReliableState
        {
            return this.GetOrAddAsync<T>(tx, new Uri(name));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name) where T : IReliableState
        {
            return this.GetOrAddAsync<T>(tx, name, this.config.DefaultOperationTimeout);
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, string name, TimeSpan timeout) where T : IReliableState
        {
            return this.GetOrAddAsync<T>(tx, new Uri(name), timeout);
        }

        public async Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name, TimeSpan timeout) where T : IReliableState
        {
            ConditionalValue<IReliableState> result = await this.TryCreateOrGetMetricReliableCollectionAsync(tx, typeof(T), name, timeout);

            return result.HasValue
                ? (T) result.Value
                : await this.stateManagerReplica.GetOrAddAsync<T>(tx, name, timeout);
        }

        public Task RemoveAsync(string name)
        {
            return this.RemoveAsync(new Uri(name), this.config.DefaultOperationTimeout);
        }

        public Task RemoveAsync(Uri name)
        {
            return this.RemoveAsync(name, this.config.DefaultOperationTimeout);
        }

        public Task RemoveAsync(ITransaction tx, string name)
        {
            return this.RemoveAsync(tx, new Uri(name));
        }

        public Task RemoveAsync(string name, TimeSpan timeout)
        {
            return this.RemoveAsync(new Uri(name), timeout);
        }

        public Task RemoveAsync(Uri name, TimeSpan timeout)
        {
            return this.stateManagerReplica.RemoveAsync(name, timeout);
        }

        public Task RemoveAsync(ITransaction tx, Uri name)
        {
            return this.RemoveAsync(tx, name, this.config.DefaultOperationTimeout);
        }

        public Task RemoveAsync(ITransaction tx, string name, TimeSpan timeout)
        {
            return this.RemoveAsync(tx, new Uri(name), timeout);
        }

        public Task RemoveAsync(ITransaction tx, Uri name, TimeSpan timeout)
        {
            return this.stateManagerReplica.RemoveAsync(tx, name, timeout);
        }

        public bool TryAddStateSerializer<T>(IStateSerializer<T> stateSerializer)
        {
            throw new NotSupportedException();
        }

        public Task<ConditionalValue<T>> TryGetAsync<T>(string name) where T : IReliableState
        {
            return this.TryGetAsync<T>(new Uri(name));
        }

        public async Task<ConditionalValue<T>> TryGetAsync<T>(Uri name) where T : IReliableState
        {
            using (ITransaction tx = this.stateManagerReplica.CreateTransaction())
            {
                ConditionalValue<IReliableState> result = await this.TryGetMetricReliableCollectionAsync(tx, typeof(T), name);

                await tx.CommitAsync();

                return result.HasValue
                    ? new ConditionalValue<T>(true, (T) result.Value)
                    : await this.stateManagerReplica.TryGetAsync<T>(name);
            }
        }

        public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            return this.stateManagerReplica.BackupAsync(backupCallback);
        }

        public Task BackupAsync(
            BackupOption option, TimeSpan timeout, CancellationToken cancellationToken, Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            return this.stateManagerReplica.BackupAsync(option, timeout, cancellationToken, backupCallback);
        }

        public Task RestoreAsync(string backupFolderPath)
        {
            return this.stateManagerReplica.RestoreAsync(backupFolderPath);
        }

        public Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy, CancellationToken cancellationToken)
        {
            return this.stateManagerReplica.RestoreAsync(backupFolderPath, restorePolicy, cancellationToken);
        }

        /// <summary>
        /// This does nothing.
        /// </summary>
        /// <param name="initializationParameters"></param>
        void IStateProviderReplica.Initialize(StatefulServiceInitializationParameters initializationParameters)
        {
            this.stateManagerReplica.Initialize(initializationParameters);
        }

        /// <summary>
        /// When the replica opens, saves the partition info.
        /// </summary>
        /// <param name="openMode"></param>
        /// <param name="partition"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IReplicator> IStateProviderReplica.OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition, CancellationToken cancellationToken)
        {
            this.partition = partition;

            return this.stateManagerReplica.OpenAsync(openMode, partition, cancellationToken);
        }

        /// <summary> 
        /// On primary and active secondary, start reporting aggregated load from all collections.
        /// Each collection needs to have load metrics available on both primary and active secondary replicas
        /// Metric collections are created on-demand when requested from the state manager, 
        /// which means they do not always exist on secondaries.
        /// </summary>
        /// <param name="newRole"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task IStateProviderReplica.ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            if (newRole == ReplicaRole.Primary || newRole == ReplicaRole.ActiveSecondary)
            {
                // For role transitions: P -> AS or AS -> P, the task should already be running.
                // It's the same task for P and AS, so let it continue to run.
                // If the task is null or not running, start a new one.
                if (this.reportTask == null ||
                    (this.reportTask.IsCanceled ||
                     this.reportTask.IsCompleted ||
                     this.reportTask.IsFaulted))
                {
                    this.reportTaskCancellation = new CancellationTokenSource();
                    this.reportTask = this.StartReportingMetricsAsync(this.reportTaskCancellation.Token);
                }
            }
            else
            {
                // For any role transition away from P or AS, cancel the reporting task.
                await this.CancelMetricReportingAsync();
            }

            await this.stateManagerReplica.ChangeRoleAsync(newRole, cancellationToken);
        }

        /// <summary>
        /// Cancels the metric reporting task and closes the state manager.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task IStateProviderReplica.CloseAsync(CancellationToken cancellationToken)
        {
            await this.CancelMetricReportingAsync();

            await this.stateManagerReplica.CloseAsync(cancellationToken);
        }

        /// <summary>
        /// Cancels the metric reporting task and aborts the state manager.
        /// </summary>
        void IStateProviderReplica.Abort()
        {
            Task t = this.CancelMetricReportingAsync();

            this.stateManagerReplica.Abort();
        }

        /// <summary>
        /// Cancels the metric reporting task and waits for it to complete.
        /// </summary>
        /// <returns></returns>
        private async Task CancelMetricReportingAsync()
        {
            if (this.reportTask != null &&
                this.reportTaskCancellation != null &&
                !this.reportTaskCancellation.IsCancellationRequested)
            {
                try
                {
                    this.reportTaskCancellation.Cancel();

                    await this.reportTask.ConfigureAwait(false);
                }
                catch
                {
                    // log any errors from the reporting task but otherwise let them go
                }
                finally
                {
                    try
                    {
                        this.reportTaskCancellation.Dispose();
                        this.reportTask.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Starts a metric report task that loops infinitely,
        /// reporting aggregated load metrics from all collections in the state manager on each iteration.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task StartReportingMetricsAsync(CancellationToken cancellationToken)
        {
            return Task.Run(
                async () =>
                {
                    MetricAggregator aggregator = new MetricAggregator();

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            IEnumerable<LoadMetric> loadMetrics = await aggregator.Aggregate(this, cancellationToken);

                            if (loadMetrics.Any())
                            {
                                this.partition.ReportLoad(loadMetrics);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (FabricNotReadableException)
                        {
                            // simple trace
                        }
                        catch (TimeoutException)
                        {
                            // simple trace
                        }
                        catch (Exception ex)
                        {
                            // full stack trace
                        }

                        await Task.Delay(this.config.ReportInterval, cancellationToken);
                    }
                },
                cancellationToken);
        }
        
        /// <summary>
        /// Inner GetOrAdd for a collection.
        /// This method adds new metric collection type information for new collections as part of the same transaction.
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private async Task<ConditionalValue<IReliableState>> TryCreateOrGetMetricReliableCollectionAsync(ITransaction tx, Type type, Uri name, TimeSpan timeout)
        {
            if (type.GetGenericTypeDefinition() == typeof(IReliableDictionary<,>))
            {
                // Store the given type information for the collection with the given name.
                // The type information is used to recreate the collection during enumeration.
                IReliableDictionary <string, string> metricDictionaryTypes = await this.GetTypeNameDictionaryAsync();

                if (!(await metricDictionaryTypes.ContainsKeyAsync(tx, name.ToString(), LockMode.Update)))
                {
                    await metricDictionaryTypes.AddAsync(tx, name.ToString(), type.AssemblyQualifiedName);
                }

                IReliableDictionary<BinaryValue, BinaryValue> innerStore =
                    await this.stateManagerReplica.GetOrAddAsync<IReliableDictionary<BinaryValue, BinaryValue>>(tx, name, timeout);

                return new ConditionalValue<IReliableState>(
                    true,
                    MetricReliableDictionaryActivator.CreateFromReliableDictionaryType(
                        type,
                        innerStore,
                        new BinaryValueConverter(name, this.serializerResolver),
                        this.config));
            }

            return new ConditionalValue<IReliableState>(false, null);
        }

        /// <summary>
        /// Inner Get for a collection.
        /// This method only attemps to get existing collections.
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<ConditionalValue<IReliableState>> TryGetMetricReliableCollectionAsync(ITransaction tx, Type type, Uri name)
        {
            if (type.GetGenericTypeDefinition() == typeof(IReliableDictionary<,>))
            {
                ConditionalValue<IReliableDictionary<BinaryValue, BinaryValue>> tryGetResult =
                    await this.stateManagerReplica.TryGetAsync<IReliableDictionary<BinaryValue, BinaryValue>>(name);

                if (tryGetResult.HasValue)
                {
                    ConditionalValue<IReliableState> result = new ConditionalValue<IReliableState>(
                        true,
                        MetricReliableDictionaryActivator.CreateFromReliableDictionaryType(
                            type,
                            tryGetResult.Value,
                            new BinaryValueConverter(name, this.serializerResolver),
                            this.config));

                    return result;
                }
            }

            return new ConditionalValue<IReliableState>(false, null);
        }

        private async Task<IReliableDictionary<string, string>> GetTypeNameDictionaryAsync()
        {
            if (this.collectionTypeNamesInstance == null)
            {
                this.collectionTypeNamesInstance =
                    await
                        this.stateManagerReplica.GetOrAddAsync<IReliableDictionary<string, string>>(
                            new Uri(MetricCollectionTypeDictionaryName));
            }

            return this.collectionTypeNamesInstance;
        }

        private void StateManagerReplica_TransactionChanged(object sender, NotifyTransactionChangedEventArgs e)
        {
            EventHandler<NotifyTransactionChangedEventArgs> handler = this.TransactionChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void StateManagerReplica_StateManagerChanged(object sender, NotifyStateManagerChangedEventArgs e)
        {
            EventHandler<NotifyStateManagerChangedEventArgs> handler = this.StateManagerChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        /// <summary>
        /// Async enumerator wrapper.
        /// </summary>
        private class MetricReliableStateManagerAsyncEnumerator : IAsyncEnumerator<IReliableState>
        {
            private readonly MetricReliableStateManager metricReliableStateManager;

            private readonly IAsyncEnumerator<IReliableState> stateManagerReplicaEnumerator;

            public MetricReliableStateManagerAsyncEnumerator(MetricReliableStateManager stateManager)
            {
                this.metricReliableStateManager = stateManager;
                this.stateManagerReplicaEnumerator = stateManager.stateManagerReplica.GetAsyncEnumerator();
            }

            public IReliableState Current { get; private set; }

            public void Dispose()
            {
                this.stateManagerReplicaEnumerator.Dispose();
            }

            /// <summary>
            /// Advances the enumerator to the next collection in the state manager and sets the Current property to it.
            /// </summary>
            /// <remarks>
            /// This works for both Metric collections and non-Metric collections even though Metric collection instances are not actually saved.
            /// This works by looking for type metadata associated with each collection name. 
            /// If type metadata is found, the type information is used to create an instance of the Metric collection,
            /// otherwise the collection is assumed to be a non-Metric collection and is returned as-is.
            /// The enumerator also skips over the collections that are used to store this metadata.
            /// </remarks>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                while (await this.stateManagerReplicaEnumerator.MoveNextAsync(cancellationToken))
                {
                    // skip over collections used internally to store metadata.
                    if (String.Equals(this.stateManagerReplicaEnumerator.Current.Name.Scheme, MetricMetadataStoreScheme))
                    {
                        continue;
                    }

                    using (ITransaction tx = this.metricReliableStateManager.CreateTransaction())
                    {
                        IReliableDictionary<string, string> collectionTypeNames = await this.metricReliableStateManager.GetTypeNameDictionaryAsync();

                        ConditionalValue<string> typeNameResult =
                            await collectionTypeNames.TryGetValueAsync(tx, this.stateManagerReplicaEnumerator.Current.Name.ToString());

                        if (typeNameResult.HasValue)
                        {
                            Uri name = this.stateManagerReplicaEnumerator.Current.Name;
                            Type type = Type.GetType(typeNameResult.Value);


                            ConditionalValue<IReliableState> result =
                                await
                                    this.metricReliableStateManager.TryGetMetricReliableCollectionAsync(
                                        tx,
                                        type,
                                        name);

                            if (result.HasValue)
                            {
                                this.Current = result.Value;
                                return true;
                            }
                        }
                    }

                    this.Current = this.stateManagerReplicaEnumerator.Current;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                this.stateManagerReplicaEnumerator.Reset();
            }
        }
    }
}