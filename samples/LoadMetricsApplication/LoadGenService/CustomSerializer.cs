using MetricReliableCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MetricReliableCollections.ReliableStateSerializers;

namespace LoadGenService
{
    internal class CustomSerializerResolver : IReliableStateSerializerResolver
    {
        public IReliableStateSerializer<T> Resolve<T>(Uri id, SerializationIntent purpose)
        {
            if (typeof(T)  == typeof( byte[]))
            {
                return new CustomSerializer() as IReliableStateSerializer<T>;
            }

            return new JsonReliableStateSerializer<T>();
        }
    }

    internal class CustomSerializer : IReliableStateSerializer<byte[]>
    {
        public byte[] Deserialize(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            return reader.ReadBytes((int)stream.Length);
        }

        public void Serialize(Stream stream, byte[] value)
        {
            stream.Write(value, 0, value.Length);
        }
    }
}
