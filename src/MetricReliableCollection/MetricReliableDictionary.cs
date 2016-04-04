// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Data.Notifications;
    using System.Fabric;
    internal static class MetricReliableDictionaryActivator
    {
        internal static IReliableState CreateFromReliableDictionaryType(
            Type type, IReliableDictionary<BinaryValue, BinaryValue> innerStore, IMetricSink metricSink, BinaryValueConverter converter)
        {
            return (IReliableState) Activator.CreateInstance(
                typeof(MetricReliableDictionary<,>).MakeGenericType(
                    type.GetGenericArguments()),
                innerStore,
                metricSink,
                converter);
        }
    }

    internal class MetricReliableDictionary<TKey, TValue> : IReliableDictionary<TKey, TValue>
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        private const string MemoryMetricName = "BytesMemory";

        private const string DiskMetricName = "BytesDisk";

        private readonly IReliableDictionary<BinaryValue, BinaryValue> store;

        private readonly BinaryValueConverter converter;

        private readonly IMetricSink metricSink;

        public MetricReliableDictionary(
            IReliableDictionary<BinaryValue, BinaryValue> store, IMetricSink metricSink, BinaryValueConverter converter)
        {
            this.store = store;
            this.metricSink = metricSink;
            this.converter = converter;
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
            return this.AddAndReportLoadAsync(tx, key, value, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public Task AddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.AddAndReportLoadAsync(tx, key, value, timeout, cancellationToken);
        }

        private Task AddAndReportLoadAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            BinaryValue binaryKey = this.converter.Serialize<TKey>(key);
            BinaryValue binaryValue = this.converter.Serialize<TValue>(value);

            return Task.WhenAll(
                this.store.AddAsync(tx, binaryKey, binaryValue, timeout, cancellationToken),
                this.AddLoad(tx, binaryKey, binaryValue, timeout, cancellationToken));
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            return this.AddOrUpdateAsync(tx, key, addValue, updateValueFactory, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public Task<TValue> AddOrUpdateAsync(ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            return this.AddOrUpdateAsync(tx, key, addValueFactory, updateValueFactory, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public async Task<TValue> AddOrUpdateAsync(
            ITransaction tx, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            TValue result;
            ConditionalValue<BinaryValue> tryGetResult = await this.store.TryGetValueAsync(tx, this.converter.Serialize<TKey>(key), LockMode.Update, timeout, cancellationToken);

            if (tryGetResult.HasValue)
            {
                result = updateValueFactory(key, this.converter.Deserialize<TValue>(tryGetResult.Value));
                await this.SetAndReportLoadAsync(tx, key, result, timeout, cancellationToken);
            }
            else
            {
                result = addValue;
                await this.AddAndReportLoadAsync(tx, key, result, timeout, cancellationToken);
            }

            return result;
        }

        public async Task<TValue> AddOrUpdateAsync(
            ITransaction tx, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            TValue result;
            ConditionalValue<BinaryValue> tryGetResult = await this.store.TryGetValueAsync(tx, this.converter.Serialize<TKey>(key), LockMode.Update, timeout, cancellationToken);

            if (tryGetResult.HasValue)
            {
                result = updateValueFactory(key, this.converter.Deserialize<TValue>(tryGetResult.Value));
                await this.SetAsync(tx, key, result, timeout, cancellationToken);
            }
            else
            {
                result = addValueFactory(key);
                await this.AddAndReportLoadAsync(tx, key, result, timeout, cancellationToken);
            }

            return result;
        }

        public Task ClearAsync()
        {
            return this.ClearAsync(TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public Task ClearAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key)
        {
            return this.ContainsKeyAsync(tx, key, LockMode.Default, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public Task<bool> ContainsKeyAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            return this.ContainsKeyAsync(tx, key, lockMode, TimeSpan.FromSeconds(4), CancellationToken.None);
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
            return this.GetOrAddAsync(tx, key, value, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, Func<TKey, TValue> valueFactory)
        {
            return this.GetOrAddAsync(tx, key, valueFactory, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public async Task<TValue> GetOrAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConditionalValue<BinaryValue> tryGetResult = await this.store.TryGetValueAsync(tx, this.converter.Serialize<TKey>(key), LockMode.Update, timeout, cancellationToken);

            if (tryGetResult.HasValue)
            {
                return this.converter.Deserialize<TValue>(tryGetResult.Value);
            }
            else
            {
                await this.AddAndReportLoadAsync(tx, key, value, timeout, cancellationToken);

                return value;
            }
        }

        public async Task<TValue> GetOrAddAsync(
            ITransaction tx, TKey key, Func<TKey, TValue> valueFactory, TimeSpan timeout, CancellationToken cancellationToken)
        {
            ConditionalValue<BinaryValue> tryGetResult = await this.store.TryGetValueAsync(tx, this.converter.Serialize<TKey>(key), LockMode.Update, timeout, cancellationToken);

            if (tryGetResult.HasValue)
            {
                return this.converter.Deserialize<TValue>(tryGetResult.Value);
            }
            else
            {
                TValue value = valueFactory(key);
                await this.AddAndReportLoadAsync(tx, key, value, timeout, cancellationToken);

                return value;
            }
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value)
        {
            return this.SetAndReportLoadAsync(tx, key, value, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public Task SetAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.SetAndReportLoadAsync(tx, key, value, timeout, cancellationToken);
        }

        private async Task SetAndReportLoadAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            BinaryValue binaryKey = this.converter.Serialize<TKey>(key);
            BinaryValue binaryValue = this.converter.Serialize<TValue>(value);

            ConditionalValue<BinaryValue> tryGetResult = await this.store.TryGetValueAsync(tx, binaryKey, LockMode.Update, timeout, cancellationToken);
            
            if (tryGetResult.HasValue)
            {
                await this.RemoveLoad(tx, binaryKey, tryGetResult.Value, timeout, cancellationToken);
            }

            await this.store.SetAsync(tx, binaryKey, binaryValue, timeout, cancellationToken);
            await this.AddLoad(tx, binaryKey, binaryValue, timeout, cancellationToken);
        }


        public Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value)
        {
            return this.TryAddAsync(tx, key, value, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public async Task<bool> TryAddAsync(ITransaction tx, TKey key, TValue value, TimeSpan timeout, CancellationToken cancellationToken)
        {
            BinaryValue binaryKey = this.converter.Serialize<TKey>(key);
            BinaryValue binaryValue = this.converter.Serialize<TValue>(value);

            bool result = await this.store.TryAddAsync(tx, binaryKey, binaryValue, timeout, cancellationToken);
            await this.AddLoad(tx, binaryKey, binaryValue, timeout, cancellationToken);

            return result;
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key)
        {
            return this.TryGetValueAsync(tx, key, LockMode.Default, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public Task<ConditionalValue<TValue>> TryGetValueAsync(ITransaction tx, TKey key, LockMode lockMode)
        {
            return this.TryGetValueAsync(tx, key, lockMode, TimeSpan.FromSeconds(4), CancellationToken.None);
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
            return this.TryRemoveAsync(tx, key, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public async Task<ConditionalValue<TValue>> TryRemoveAsync(ITransaction tx, TKey key, TimeSpan timeout, CancellationToken cancellationToken)
        {
            BinaryValue binaryKey = this.converter.Serialize<TKey>(key);
            ConditionalValue<BinaryValue> result = await this.store.TryRemoveAsync(tx, binaryKey, timeout, cancellationToken);

            if (result.HasValue)
            {
                await this.RemoveLoad(tx, binaryKey, result.Value, timeout, cancellationToken);
                return new ConditionalValue<TValue>(true, this.converter.Deserialize<TValue>(result.Value));
            }
            else
            {
                return new ConditionalValue<TValue>(false, default(TValue));
            }
        }

        public Task<bool> TryUpdateAsync(ITransaction tx, TKey key, TValue newValue, TValue comparisonValue)
        {
            return this.TryUpdateAsync(tx, key, newValue, comparisonValue, TimeSpan.FromSeconds(4), CancellationToken.None);
        }

        public async Task<bool> TryUpdateAsync(
            ITransaction tx, TKey key, TValue newValue, TValue comparisonValue, TimeSpan timeout, CancellationToken cancellationToken)
        {
            BinaryValue binaryKey = this.converter.Serialize<TKey>(key);
            BinaryValue binaryNewValue = this.converter.Serialize<TValue>(newValue);
            BinaryValue binaryComparisonValue = this.converter.Serialize<TValue>(comparisonValue);

            bool result = await this.store.TryUpdateAsync(
                tx,
                binaryKey,
                binaryNewValue,
                binaryComparisonValue,
                timeout,
                cancellationToken);

            if (result)
            {
                await this.RemoveLoad(tx, binaryKey, binaryNewValue, timeout, cancellationToken);
                await this.AddLoad(tx, binaryKey, binaryNewValue, timeout, cancellationToken);
            }

            return result;
        }

        private Task AddLoad(ITransaction tx, BinaryValue key, BinaryValue value, TimeSpan timeout, CancellationToken token)
        {
            if (this.metricSink != null)
            {
                LoadMetric[] metrics = new LoadMetric[2];
                metrics[0] = new LoadMetric(MemoryMetricName, key.Buffer.Length + value.Buffer.Length);
                metrics[0] = new LoadMetric(DiskMetricName, key.Buffer.Length + value.Buffer.Length);

                return this.metricSink.AddLoadAsync(tx, this.Name, metrics, timeout, token);
            }

            return Task.FromResult(false);
        }

        private Task RemoveLoad(ITransaction tx, BinaryValue key, BinaryValue value, TimeSpan timeout, CancellationToken token)
        {
            if (this.metricSink != null)
            {
                LoadMetric[] metrics = new LoadMetric[2];
                metrics[0] = new LoadMetric(MemoryMetricName, -(key.Buffer.Length + value.Buffer.Length));
                metrics[0] = new LoadMetric(DiskMetricName, -(key.Buffer.Length + value.Buffer.Length));

                return this.metricSink.AddLoadAsync(tx, this.Name, metrics, timeout, token);
            }

            return Task.FromResult(false);
        }
    }
}