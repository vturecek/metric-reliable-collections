// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System.IO;

    public interface IReliableStateSerializer<T>
    {
        void Serialize(Stream stream, T value);

        T Deserialize(Stream stream);
    }
}