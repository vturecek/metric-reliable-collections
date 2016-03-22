// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    public static class ReliableDictionaryAsyncEnumerableExtensions
    {
        public static Task ForeachAsync<TKey, TValue>(
            this IReliableDictionary<TKey, TValue> instance, ITransaction tx, CancellationToken token, Action<KeyValuePair<TKey, TValue>> doSomething)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            return ForeachAsync<TKey, TValue>(instance.CreateEnumerableAsync(tx), token, doSomething);
        }

        public static Task ForeachAsync<TKey, TValue>(
            this IReliableDictionary<TKey, TValue> instance, ITransaction tx, CancellationToken token, Func<TKey, bool> filter,
            Action<KeyValuePair<TKey, TValue>> doSomething)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            return ForeachAsync<TKey, TValue>(instance.CreateEnumerableAsync(tx, filter, EnumerationMode.Unordered), token, doSomething);
        }

        public static Task ForeachAsync<TKey, TValue>(
            this IReliableDictionary<TKey, TValue> instance, ITransaction tx, CancellationToken token, EnumerationMode enumMode,
            Action<KeyValuePair<TKey, TValue>> doSomething)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            return ForeachAsync<TKey, TValue>(instance.CreateEnumerableAsync(tx, enumMode), token, doSomething);
        }

        public static Task ForeachAsync<TKey, TValue>(
            this IReliableDictionary<TKey, TValue> instance, ITransaction tx, CancellationToken token, EnumerationMode enumMode, Func<TKey, bool> filter,
            Action<KeyValuePair<TKey, TValue>> doSomething)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            return ForeachAsync<TKey, TValue>(instance.CreateEnumerableAsync(tx, filter, enumMode), token, doSomething);
        }

        private static async Task ForeachAsync<TKey, TValue>(
            Task<IAsyncEnumerable<KeyValuePair<TKey, TValue>>> enumeratorTask, CancellationToken token, Action<KeyValuePair<TKey, TValue>> doSomething)
            where TKey : IEquatable<TKey>, IComparable<TKey>
        {
            IAsyncEnumerator<KeyValuePair<TKey, TValue>> e = (await enumeratorTask).GetAsyncEnumerator();

            try
            {
                goto Check;

                Resume:
                KeyValuePair<TKey, TValue> i = e.Current;
                {
                    doSomething(i);
                }

                Check:
                if (await e.MoveNextAsync(token))
                {
                    goto Resume;
                }
            }
            finally
            {
                if (e != null)
                {
                    e.Dispose();
                }
            }
        }
    }
}