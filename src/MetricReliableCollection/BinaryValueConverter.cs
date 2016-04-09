// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;
    using System.IO;

    internal class BinaryValueConverter
    {
        private readonly IReliableStateSerializerResolver serializerResolver;
        private readonly Uri collectionName;

        public BinaryValueConverter(Uri collectionName, IReliableStateSerializerResolver resolver)
        {
            this.serializerResolver = resolver;
            this.collectionName = collectionName;
        }

        public BinaryValue Serialize<T>(T value)
        {
            IReliableStateSerializer<T> serializer = this.serializerResolver.Resolve<T>(this.collectionName, SerializationIntent.Replication);

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, value);
                return new BinaryValue(stream.ToArray(), false);
            }
        }

        public T Deserialize<T>(BinaryValue data)
        {
            IReliableStateSerializer<T> serializer = this.serializerResolver.Resolve<T>(this.collectionName, SerializationIntent.Replication);

            using (MemoryStream stream = new MemoryStream(data.Buffer))
            {
                return serializer.Deserialize(stream);
            }
        }
    }
}