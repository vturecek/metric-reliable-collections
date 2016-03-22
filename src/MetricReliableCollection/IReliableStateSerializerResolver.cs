// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;

    public interface IReliableStateSerializerResolver
    {
        IReliableStateSerializer<T> Resolve<T>(Uri id, SerializationIntent purpose);
    }
}