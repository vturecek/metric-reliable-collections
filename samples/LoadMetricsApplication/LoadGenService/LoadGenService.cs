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
        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            return RunRandomizedLoad(cancellationToken);
        }

        private async Task RunSimpleLoad(CancellationToken cancellationToken)
        {
            IReliableDictionary<int, byte[]> dictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<int, byte[]>>("store:/test");

            for (int i = 0; i < 10000; ++i)
            {
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    await dictionary.SetAsync(tx, i, GetBlob(650));
                    await tx.CommitAsync();
                }
            }
        }

        private async Task RunRandomizedLoad(CancellationToken cancellationToken)
        {
            try
            {
                string[] dictionaries = { "store://d1", "store://d2", "premium://d1", "encrypted://aes" };

                Random random = new Random();

                int size = ((Int64RangePartitionInformation)this.Partition.PartitionInfo).LowKey % 10 == 0
                    ? this.GetHighProfile()
                    : this.GetLowProfile();

                ServiceEventSource.Current.ServiceMessage(this, "Size: {0}", size);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IReliableDictionary<int, byte[]> dictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<int, byte[]>>(dictionaries[random.Next(0, dictionaries.Length)]);

                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        await dictionary.SetAsync(tx, random.Next(0, 50), this.GetBlob(random.Next(size / 2, size)));

                        await tx.CommitAsync();
                    }

                    await Task.Delay(500, cancellationToken);
                }
               
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(this, "Oops. {0}", e.ToString());
            }
        }

        private byte[] GetBlob(int size)
        {
            Random random = new Random();

            byte[] blob = new byte[size];
            for (int i = 0; i < size; ++i)
            {
                blob[i] = (byte)random.Next();
            }

            return blob;
        }

        private int GetLowProfile()
        {
            Random random = new Random();
            return random.Next(100, 1000);
        }

        private int GetHighProfile()
        {
            Random random = new Random();
            return random.Next(30000, 50000);
        }
    }
}