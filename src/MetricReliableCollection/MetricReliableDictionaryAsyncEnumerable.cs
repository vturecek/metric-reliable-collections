// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;

    internal class MetricReliableDictionaryAsyncEnumerable<TKey, TValue> : IAsyncEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly IAsyncEnumerable<KeyValuePair<BinaryValue, BinaryValue>> enumerable;
        private readonly BinaryValueConverter converter;

        public MetricReliableDictionaryAsyncEnumerable(IAsyncEnumerable<KeyValuePair<BinaryValue, BinaryValue>> enumerable, BinaryValueConverter converter)
        {
            this.enumerable = enumerable;
            this.converter = converter;
        }

        public IAsyncEnumerator<KeyValuePair<TKey, TValue>> GetAsyncEnumerator()
        {
            return new MetricReliableDictionaryAsyncEnumerator<TKey, TValue>(this.enumerable.GetAsyncEnumerator(), this.converter);
        }
    }

    internal class MetricReliableDictionaryAsyncEnumerator<TKey, TValue> : IAsyncEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly IAsyncEnumerator<KeyValuePair<BinaryValue, BinaryValue>> enumerator;
        private readonly BinaryValueConverter converter;

        public MetricReliableDictionaryAsyncEnumerator(IAsyncEnumerator<KeyValuePair<BinaryValue, BinaryValue>> enumerator, BinaryValueConverter converter)
        {
            this.enumerator = enumerator;
            this.converter = converter;
        }

        public KeyValuePair<TKey, TValue> Current
        {
            get
            {
                return new KeyValuePair<TKey, TValue>(
                    this.converter.Deserialize<TKey>(this.enumerator.Current.Key),
                    this.converter.Deserialize<TValue>(this.enumerator.Current.Value));
            }
        }

        public void Dispose()
        {
            this.enumerator.Dispose();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return this.enumerator.MoveNextAsync(cancellationToken);
        }

        public void Reset()
        {
            this.enumerator.Reset();
        }
    }
}