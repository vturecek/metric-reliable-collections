// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadGenService
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using MetricReliableCollections;
    using MetricReliableCollections.ReliableStateSerializers;
    using Microsoft.ServiceFabric.Services.Runtime;

    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                //To use regular Reliable Collection, use this registration instead:
                //ServiceRuntime.RegisterServiceAsync(
                //    "LoadGenServiceType",
                //    context => new LoadGenService(context, new ReliableStateManager(context)))
                //    .GetAwaiter()
                //    .GetResult();

                ServiceRuntime.RegisterServiceAsync(
                    "LoadGenServiceType",
                    context => new LoadGenService(
                        context,
                        new MetricReliableStateManager(
                            context,
                            new JsonReliableStateSerializerResolver(),
                            new MetricConfiguration("MemoryKB", DataSizeUnits.Kilobytes, "DiskKB", DataSizeUnits.Kilobytes, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)))))
                    .GetAwaiter()
                    .GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(LoadGenService).Name);

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}