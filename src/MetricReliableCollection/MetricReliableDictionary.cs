// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using MetricReliableCollections.Extensions;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Data.Notifications;

    internal static class MetricReliableDictionaryActivator
    {
        internal static IReliableState CreateFromReliableDictionaryType(
            Type type, IReliableDictionary<BinaryValue, BinaryValue> innerStore, BinaryValueConverter converter, MetricConfiguration config)
        {
            return (IReliableState) Activator.CreateInstance(
                typeof(MetricReliableDictionary<,>).MakeGenericType(
                    type.GetGenericArguments()),
                innerStore,
                converter,
                config);
        }
    }

    internal class MetricReliableDictionary<TKey, TValue> : IReliableDictionary<TKey, TValue>, IMetricReliableCollection
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        private readonly IReliableDictionary<BinaryValue, BinaryValue> store;

        private readonly BinaryValueConverter converter;

        private readonly MetricConfiguration config;

        public MetricReliableDictionary(
            IReliableDictionary<BinaryValue, BinaryValue> store, BinaryValueConverter converter, MetricConfiguration config)
        {
            this.store = store;
            this.converter = converter;
            this.config = config;
        }

        public async Task<IEnumerable<DecimalLoadMetric>> GetLoadMetricsAsync(ITransaction tx, CancellationToken cancellationToken)
        {
            double total = 0;

            await this.store.ForeachAsync(tx, cancellationToken, item => { total += item.Key.Buffer.Length + item.Value.Buffer.Length; });

            return new[]
            {
                new DecimalLoadMetric(this.config.MemoryMetricName, total / (double)this.config.MemoryMetricUnits),
                new DecimalLoadMetric(this.config.DiskMetricName, total / (double)this.config.DiskMetricUnits)
            };
        }

        public Uri Name
        {
            get { return this.store.Name; }
        }

        public Func<IReliableDictionary<TKey, TValue>, NotifyDictionaryRebuildEventArgs<TKey, TValue>, Task> RebuildNotificationAsyncCallback
        {
            set { throw new NotImplementedException(); }
        }

        public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>> DictionaryChanged;

        public Task AddAsync(ITransaction tx, TKey key, TValue value)
        {
            return this.AddAsync(tx, key, value, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task AddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.store.AddAsync(tx, this.converter.Serialize<TKey>(key), this.converter.Serialize<TValue>(value), timeout, cancellationToken);
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            return this.AddOrUpdateAsync(tx, key, addValue, updateValueFactory, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            return this.AddOrUpdateAsync(tx, key, addValueFactory, updateValueFactory, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public async Task<TValue> AddOrUpdateAsync(
            ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            BinaryValue result = await this.store.AddOrUpdateAsync(
                tx,
                this.converter.Serialize<TKey>(key),
                this.converter.Serialize<TValue>(addValue),
                (k, v) => this.converter.Serialize<TValue>(updateValueFactory(this.converter.Deserialize<TKey>(k), this.converter.Deserialize<TValue>(v))),
                timeout,
                cancellationToken);

            return this.converter.Deserialize<TValue>(result);
        }

        public async Task<TValue> AddOrUpdateAsync(
            ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            BinaryValue result = await this.store.AddOrUpdateAsync(
                tx,
                this.converter.Serialize<TKey>(key),
                (k) => this.converter.Serialize<TValue>(addValueFactory(this.converter.Deserialize<TKey>(k))),
                (k, v) => this.converter.Serialize<TValue>(updateValueFactory(this.converter.Deserialize<TKey>(k), this.converter.Deserialize<TValue>(v))),
                timeout,
                cancellationToken);

            return this.converter.Deserialize<TValue>(result);
        }

        public Task ClearAsync()
        {
            return this.store.ClearAsync();
        }

        public Task ClearAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.store.ClearAsync(timeout, cancellationToken);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key)
        {
            return this.ContainsKeyAsync(tx, key, LockMode.Default, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            return this.ContainsKeyAsync(tx, key, lockMode, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.ContainsKeyAsync(tx, key, LockMode.Default, timeout, cancellationToken);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.store.ContainsKeyAsync(tx, this.converter.Serialize<TKey>(key), lockMode, timeout, cancellationToken);
        }

        public async Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn)
        {
            return new MetricReliableDictionaryAsyncEnumerable<TKey, TValue>(
                await this.store.CreateEnumerableAsync(txn),
                this.converter);
        }

        public async Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(ITransaction txn, EnumerationMode enumerationMode)
        {
            return new MetricReliableDictionaryAsyncEnumerable<TKey, TValue>(
                await this.store.CreateEnumerableAsync(txn, enumerationMode),
                this.converter);
        }

        public async Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> CreateEnumerableAsync(
            ITransaction txn, Func<TKey, bool> filter, EnumerationMode enumerationMode)
        {
            return new MetricReliableDictionaryAsyncEnumerable<TKey, TValue>(
                await this.store.CreateEnumerableAsync(txn, key => filter(this.converter.Deserialize<TKey>(key)), enumerationMode),
                this.converter);
        }

        public Task<long> GetCountAsync(ITransaction tx)
        {
            return this.GetCountAsync(tx);
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value)
        {
            return this.GetOrAddAsync(tx, key, value, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory)
        {
            return this.GetOrAddAsync(tx, key, valueFactory, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public async Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            await this.store.GetOrAddAsync(tx, this.converter.Serialize<TKey>(key), this.converter.Serialize<TValue>(value), timeout, cancellationToken);

            return value;
        }

        public async Task<TValue> GetOrAddAsync(
            ITransaction tx, TKey key, Func<TKey, TValue> valueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            BinaryValue result = await this.store.GetOrAddAsync(
                tx,
                this.converter.Serialize<TKey>(key),
                k => this.converter.Serialize<TValue>(valueFactory(this.converter.Deserialize<TKey>(k))),
                timeout,
                cancellationToken);

            return this.converter.Deserialize<TValue>(result);
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value)
        {
            return this.SetAsync(tx, key, value, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.store.SetAsync(tx, this.converter.Serialize<TKey>(key), this.converter.Serialize<TValue>(value), timeout, cancellationToken);
        }

        public Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value)
        {
            return this.TryAddAsync(tx, key, value, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.store.TryAddAsync(tx, this.converter.Serialize<TKey>(key), this.converter.Serialize<TValue>(value), timeout, cancellationToken);
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key)
        {
            return this.TryGetValueAsync(tx, key, LockMode.Default, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            return this.TryGetValueAsync(tx, key, lockMode, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.TryGetValueAsync(tx, key, LockMode.Default, timeout, cancellationToken);
        }

        public async Task<ConditionalValue<TValue>> TryGetValueAsync(
            ITransaction tx, TKey key, LockMode lockMode, TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConditionalValue<BinaryValue> result =
                await this.store.TryGetValueAsync(tx, this.converter.Serialize<TKey>(key), lockMode, timeout, cancellationToken);

            return result.HasValue
                ? new ConditionalValue<TValue>(true, this.converter.Deserialize<TValue>(result.Value))
                : new ConditionalValue<TValue>(false, default(TValue));
        }

        public Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key)
        {
            return this.TryRemoveAsync(tx, key, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public async Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConditionalValue<BinaryValue> result = await this.store.TryRemoveAsync(tx, this.converter.Serialize<TKey>(key), timeout, cancellationToken);

            return result.HasValue
                ? new ConditionalValue<TValue>(true, this.converter.Deserialize<TValue>(result.Value))
                : new ConditionalValue<TValue>(false, default(TValue));
        }

        public Task<bool> TryUpdateAsync(ITransaction tx, TKey key, TValue newValue, TValue comparisonValue)
        {
            return this.TryUpdateAsync(tx, key, newValue, comparisonValue, this.config.DefaultOperationTimeout, CancellationToken.None);
        }

        public Task<bool> TryUpdateAsync(
            ITransaction tx, TKey key, TValue newValue, TValue comparisonValue, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.store.TryUpdateAsync(
                tx,
                this.converter.Serialize<TKey>(key),
                this.converter.Serialize<TValue>(newValue),
                this.converter.Serialize<TValue>(comparisonValue),
                timeout,
                cancellationToken);
        }
    }
}