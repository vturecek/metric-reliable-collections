﻿// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Tests.Mocks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Data.Notifications;

    public class MockReliableStateManager : IReliableStateManagerReplica
    {
        private ConcurrentDictionary<Uri, IReliableState> store = new ConcurrentDictionary<Uri, IReliableState>();

        private Dictionary<Type, Type> dependencyMap = new Dictionary<Type, Type>()
        {
            {typeof(IReliableDictionary<,>), typeof(MockReliableDictionary<,>)},
            {typeof(IReliableQueue<>), typeof(MockReliableQueue<>)}
        };

        public Func<CancellationToken, Task<bool>> OnDataLossAsync
        {
            set { throw new NotImplementedException(); }
        }

        public event EventHandler<NotifyTransactionChangedEventArgs> TransactionChanged;

        public event EventHandler<NotifyStateManagerChangedEventArgs> StateManagerChanged;

        public ITransaction CreateTransaction()
        {
            return new MockTransaction();
        }

        public Task RemoveAsync(string name)
        {
            IReliableState result;
            this.store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, string name)
        {
            IReliableState result;
            this.store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(string name, TimeSpan timeout)
        {
            IReliableState result;
            this.store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, string name, TimeSpan timeout)
        {
            IReliableState result;
            this.store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(Uri name)
        {
            IReliableState result;
            this.store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(Uri name, TimeSpan timeout)
        {
            IReliableState result;
            this.store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, Uri name)
        {
            IReliableState result;
            this.store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, Uri name, TimeSpan timeout)
        {
            IReliableState result;
            this.store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task<ConditionalValue<T>> TryGetAsync<T>(string name) where T : IReliableState
        {
            IReliableState item;
            bool result = this.store.TryGetValue(this.ToUri(name), out item);

            return Task.FromResult(new ConditionalValue<T>(result, (T) item));
        }

        public Task<ConditionalValue<T>> TryGetAsync<T>(Uri name) where T : IReliableState
        {
            IReliableState item;
            bool result = this.store.TryGetValue(name, out item);

            return Task.FromResult(new ConditionalValue<T>(result, (T) item));
        }

        public Task<T> GetOrAddAsync<T>(string name) where T : IReliableState
        {
            return Task.FromResult((T) this.store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T), this.ToUri(name))));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, string name) where T : IReliableState
        {
            return Task.FromResult((T) this.store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T), this.ToUri(name))));
        }

        public Task<T> GetOrAddAsync<T>(string name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T) this.store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T), this.ToUri(name))));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, string name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T) this.store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T), this.ToUri(name))));
        }

        public Task<T> GetOrAddAsync<T>(Uri name) where T : IReliableState
        {
            return Task.FromResult((T) this.store.GetOrAdd(name, this.GetDependency(typeof(T), name)));
        }

        public Task<T> GetOrAddAsync<T>(Uri name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T) this.store.GetOrAdd(name, this.GetDependency(typeof(T), name)));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name) where T : IReliableState
        {
            return Task.FromResult((T) this.store.GetOrAdd(name, this.GetDependency(typeof(T), name)));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T) this.store.GetOrAdd(name, this.GetDependency(typeof(T), name)));
        }

        public bool TryAddStateSerializer<T>(IStateSerializer<T> stateSerializer)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerator<IReliableState> GetAsyncEnumerator()
        {
            return new MockAsyncEnumerator<IReliableState>(this.store.Values.GetEnumerator());
        }

        public void Initialize(StatefulServiceInitializationParameters initializationParameters)
        {
        }

        public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReplicator>(null);
        }

        public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public void Abort()
        {
        }

        public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            throw new NotImplementedException();
        }

        public Task BackupAsync(
            BackupOption option, TimeSpan timeout, CancellationToken cancellationToken, Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            throw new NotImplementedException();
        }

        public Task RestoreAsync(string backupFolderPath)
        {
            throw new NotImplementedException();
        }

        public Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void SetMock<TKey, TValue>(Uri name, IReliableDictionary<TKey, TValue> dictionary)
            where TKey : IComparable<TKey>, IEquatable<TKey>
        {
            this.store[name] = dictionary;
        }

        public Task ClearAsync(ITransaction tx)
        {
            this.store.Clear();
            return Task.FromResult(true);
        }

        public Task ClearAsync()
        {
            this.store.Clear();
            return Task.FromResult(true);
        }

        private IReliableState GetDependency(Type t, Uri name)
        {
            Type mockType = this.dependencyMap[t.GetGenericTypeDefinition()];

            return (IReliableState) Activator.CreateInstance(mockType.MakeGenericType(t.GetGenericArguments()), name);
        }

        private Uri ToUri(string name)
        {
            return new Uri("mock://" + name, UriKind.Absolute);
        }
    }
}