// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections.ReliableStateSerializers
{
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;

    public class JsonReliableStateSerializer<T> : IReliableStateSerializer<T>
    {
        private static JsonSerializer j = new JsonSerializer();


        public T Deserialize(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                return j.Deserialize<T>(new JsonTextReader(reader));
            }
        }


        public void Serialize(Stream stream, T value)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                j.Serialize(writer, value);
            }
        }
    }
}