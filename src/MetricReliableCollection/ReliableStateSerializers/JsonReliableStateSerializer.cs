// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.ReliableStateSerializers
{
    using System.IO;
    using Newtonsoft.Json;

    public class JsonReliableStateSerializer<T> : IReliableStateSerializer<T>
    {
        private static JsonSerializer j = new JsonSerializer();


        public T Deserialize(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                return j.Deserialize<T>(new JsonTextReader(reader));
            }
        }


        public void Serialize(Stream stream, T value)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                j.Serialize(writer, value);
            }
        }
    }
}