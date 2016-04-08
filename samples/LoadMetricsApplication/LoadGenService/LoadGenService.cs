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
                string[] dictionaries = {"store://d1", "store://d2", "premium://d1", "encrypted://aes"};

                Random random = new Random();
                double profile = random.NextDouble();

                int size = profile < 0.5
                    ? this.GetLowProfile()
                    : this.GetHighProfile();

                ServiceEventSource.Current.ServiceMessage(this, "Size: {0}", size);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IReliableDictionary<int, int[]> dictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int[]>>(dictionaries[random.Next(0, dictionaries.Length)]);

                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        await dictionary.SetAsync(tx, random.Next(0, size), this.GetBlob(random.Next(size/2, size)));

                        await tx.CommitAsync();
                    }

                    await Task.Delay(500, cancellationToken);
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(this, "Oops. {0}", e.ToString());
            }
        }

        private int[] GetBlob(int size)
        {
            Random random = new Random();

            int[] blob = new int[size];
            for (int i = 0; i < size; ++i)
            {
                blob[i] = random.Next();
            }

            return blob;
        }

        private int GetLowProfile()
        {
            Random random = new Random();
            return random.Next(2, 20);
        }

        private int GetHighProfile()
        {
            Random random = new Random();
            return random.Next(1000, 1500);
        }
    }
}