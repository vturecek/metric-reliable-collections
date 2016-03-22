// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.ReliableStateSerializers
{
    using System;

    public class JsonReliableStateSerializerResolver : IReliableStateSerializerResolver
    {
        public IReliableStateSerializer<T> Resolve<T>(Uri id, SerializationIntent purpose)
        {
            return new JsonReliableStateSerializer<T>();
        }
    }
}