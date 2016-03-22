// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadGenService
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using MetricReliableCollections.Extensions;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class LoadGenService : StatefulService
    {
        public LoadGenService(StatefulServiceContext context, IReliableStateManagerReplica stateManagerReplica)
            : base(context, stateManagerReplica)
        {
        }


        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                IReliableDictionary<int, int> myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>("store://myDictionary");

                ServiceEventSource.Current.ServiceMessage(this, "Adding 1000 items..");
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        await myDictionary.SetAsync(tx, i, i);
                    }

                    await tx.CommitAsync();
                }
                ServiceEventSource.Current.ServiceMessage(this, "Done.");

                ServiceEventSource.Current.ServiceMessage(this, "Summing all values");
                int sum = 0;
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    await myDictionary.ForeachAsync(tx, cancellationToken, item => { sum += item.Value; });
                }
                ServiceEventSource.Current.ServiceMessage(this, "Done. Result: {0}", sum);

                ServiceEventSource.Current.ServiceMessage(this, "Summing filtered values");
                sum = 0;
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    await myDictionary.ForeachAsync(tx, cancellationToken, key => key < 100, item => { sum += item.Value; });
                }
                ServiceEventSource.Current.ServiceMessage(this, "Done. Result: {0}", sum);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(this, "Oops. {0}", e.ToString());
            }
        }
    }
}