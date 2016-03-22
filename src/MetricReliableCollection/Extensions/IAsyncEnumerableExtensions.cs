// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.Extensions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;

    public static class IAsyncEnumerableExtensions
    {
        public static async Task ForeachAsync<T>(this IAsyncEnumerable<T> instance, CancellationToken token, Action<T> doSomething)
        {
            IAsyncEnumerator<T> e = instance.GetAsyncEnumerator();

            try
            {
                goto Check;

                Resume:
                T i = e.Current;
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
                // IEnumerator<T> inherits from IDisposable, so no need to use 'as' keyword.
                if (e != null)
                {
                    e.Dispose();
                }
            }
        }
    }
}