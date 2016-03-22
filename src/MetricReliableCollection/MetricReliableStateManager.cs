// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;
    using System.Collections.Concurrent;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Data.Notifications;

    public class MetricReliableStateManager : IReliableStateManagerReplica
    {
        /// <summary>
        /// collections that are used for metadata use this URI scheme for their name.
        /// </summary>
        private const string MetricMetadataStoreScheme = "metricreliablestatemetadata";

        private const string MetricCollectionTypeDictionaryName = "metricreliablestatemetadata://metriccollectiontypes";

        private static readonly TimeSpan DefaultReportInterval = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);

        private readonly ConcurrentDictionary<Uri, object> collectionCache;

        private readonly IReliableStateManagerReplica stateManagerReplica;

        private readonly IReliableStateSerializerResolver serializerResolver;

        private IStatefulServicePartition partition;

        private CancellationTokenSource reportTaskCancellation;

        private Task reportTask;

        public MetricReliableStateManager(StatefulServiceContext context, IReliableStateSerializerResolver serializerResolver)
        {
            this.stateManagerReplica = new ReliableStateManager(
                context,
                new ReliableStateManagerConfiguration(
                    onInitializeStateSerializersEvent: () =>
                        Task.FromResult(this.stateManagerReplica.TryAddStateSerializer<BinaryValue>(new BinaryValueStateSerializer()))));

            this.serializerResolver = serializerResolver;
            this.collectionCache = new ConcurrentDictionary<Uri, object>();
        }

        internal MetricReliableStateManager(
            StatefulServiceContext context, IReliableStateSerializerResolver serializerResolver, IReliableStateManagerReplica stateManager)
        {
            this.stateManagerReplica = stateManager;
            this.serializerResolver = serializerResolver;
            this.collectionCache = new ConcurrentDictionary<Uri, object>();
        }

        public event EventHandler<NotifyStateManagerChangedEventArgs> StateManagerChanged;

        public event EventHandler<NotifyTransactionChangedEventArgs> TransactionChanged;


        public Func<CancellationToken, Task<bool>> OnDataLossAsync
        {
            set { this.stateManagerReplica.OnDataLossAsync = value; }
        }

        public Task ClearAsync()
        {
            return this.stateManagerReplica.ClearAsync();
        }

        public Task ClearAsync(ITransaction tx)
        {
            return this.stateManagerReplica.ClearAsync(tx);
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
            return this.GetOrAddAsync<T>(name, DefaultTimeout);
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
            return this.GetOrAddAsync<T>(tx, name, DefaultTimeout);
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
            return this.RemoveAsync(new Uri(name), DefaultTimeout);
        }

        public Task RemoveAsync(Uri name)
        {
            return this.RemoveAsync(name, DefaultTimeout);
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
            return this.RemoveAsync(tx, name, DefaultTimeout);
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

        public void Initialize(StatefulServiceInitializationParameters initializationParameters)
        {
            this.stateManagerReplica.Initialize(initializationParameters);
        }

        public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition, CancellationToken cancellationToken)
        {
            this.partition = partition;

            return this.stateManagerReplica.OpenAsync(openMode, partition, cancellationToken);
        }

        public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            return this.stateManagerReplica.ChangeRoleAsync(newRole, cancellationToken);
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            await this.stateManagerReplica.CloseAsync(cancellationToken);
        }

        public void Abort()
        {
            this.stateManagerReplica.Abort();
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

        private async Task UpdateMetricCollectionTypes(ITransaction tx, Type type, Uri name, TimeSpan timeout)
        {
            IReliableDictionary<string, string> metricDictionaryTypes =
                await this.stateManagerReplica.GetOrAddAsync<IReliableDictionary<string, string>>(tx, new Uri(MetricCollectionTypeDictionaryName), timeout);

            if (!(await metricDictionaryTypes.ContainsKeyAsync(tx, name.ToString(), LockMode.Update)))
            {
                await metricDictionaryTypes.AddAsync(tx, name.ToString(), type.AssemblyQualifiedName);
            }
        }

        private static Uri CreateMetricStoreUri(Uri name)
        {
            UriBuilder builder = new UriBuilder(name)
            {
                Scheme = MetricMetadataStoreScheme
            };

            return builder.Uri;
        }

        private async Task<ConditionalValue<IReliableState>> TryCreateOrGetMetricReliableCollectionAsync(ITransaction tx, Type type, Uri name, TimeSpan timeout)
        {
            if (type.GetGenericTypeDefinition() == typeof(IReliableDictionary<,>))
            {
                await this.UpdateMetricCollectionTypes(tx, type, name, timeout);

                IReliableDictionary<BinaryValue, BinaryValue> innerStore =
                    await this.stateManagerReplica.GetOrAddAsync<IReliableDictionary<BinaryValue, BinaryValue>>(tx, name, timeout);

                IReliableDictionary<string, long> metricStore =
                    await this.stateManagerReplica.GetOrAddAsync<IReliableDictionary<string, long>>(tx, CreateMetricStoreUri(name), timeout);

                return new ConditionalValue<IReliableState>(
                    true,
                    MetricReliableDictionaryActivator.CreateFromReliableDictionaryType(
                        type,
                        innerStore,
                        metricStore,
                        new BinaryValueConverter(name, this.serializerResolver)));
            }

            return new ConditionalValue<IReliableState>(false, null);
        }

        private async Task<ConditionalValue<IReliableState>> TryGetMetricReliableCollectionAsync(ITransaction tx, Type type, Uri name)
        {
            if (type.GetGenericTypeDefinition() == typeof(IReliableDictionary<,>))
            {
                ConditionalValue<IReliableDictionary<BinaryValue, BinaryValue>> tryGetResult =
                    await this.stateManagerReplica.TryGetAsync<IReliableDictionary<BinaryValue, BinaryValue>>(name);

                if (tryGetResult.HasValue)
                {
                    await this.UpdateMetricCollectionTypes(tx, type, name, DefaultTimeout);

                    IReliableDictionary<string, long> metricStore =
                        await this.stateManagerReplica.GetOrAddAsync<IReliableDictionary<string, long>>(tx, CreateMetricStoreUri(name));

                    ConditionalValue<IReliableState> result = new ConditionalValue<IReliableState>(
                        true,
                        MetricReliableDictionaryActivator.CreateFromReliableDictionaryType(
                            type,
                            tryGetResult.Value,
                            metricStore,
                            new BinaryValueConverter(name, this.serializerResolver)));

                    return result;
                }
            }

            return new ConditionalValue<IReliableState>(false, null);
        }

        private class MetricReliableStateManagerAsyncEnumerator : IAsyncEnumerator<IReliableState>
        {
            private readonly MetricReliableStateManager metricReliableStateManager;

            private readonly IAsyncEnumerator<IReliableState> stateManagerReplicaEnumerator;

            private IReliableDictionary<string, string> collectionTypeNamesInstance;

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

            public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                while (await this.stateManagerReplicaEnumerator.MoveNextAsync(cancellationToken))
                {
                    if (String.Equals(this.stateManagerReplicaEnumerator.Current.Name.Scheme, MetricMetadataStoreScheme))
                    {
                        continue;
                    }

                    using (ITransaction tx = this.metricReliableStateManager.CreateTransaction())
                    {
                        IReliableDictionary<string, string> collectionTypeNames = await this.GetTypeNameDictionaryAsync();

                        ConditionalValue<string> typeNameResult =
                            await collectionTypeNames.TryGetValueAsync(tx, this.stateManagerReplicaEnumerator.Current.Name.ToString());

                        if (typeNameResult.HasValue)
                        {
                            Uri name = this.stateManagerReplicaEnumerator.Current.Name;
                            Type type = Type.GetType(typeNameResult.Value);

                            ConditionalValue<IReliableState> result =
                                await this.metricReliableStateManager.TryCreateOrGetMetricReliableCollectionAsync(tx, type, name, DefaultTimeout);

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

            private async Task<IReliableDictionary<string, string>> GetTypeNameDictionaryAsync()
            {
                if (this.collectionTypeNamesInstance == null)
                {
                    this.collectionTypeNamesInstance =
                        await
                            this.metricReliableStateManager.stateManagerReplica.GetOrAddAsync<IReliableDictionary<string, string>>(
                                new Uri(MetricCollectionTypeDictionaryName));
                }

                return this.collectionTypeNamesInstance;
            }
        }
    }
}