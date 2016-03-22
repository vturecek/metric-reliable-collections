// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;
    using System.IO;
    using Microsoft.ServiceFabric.Data;

    internal class BinaryValueStateSerializer : IStateSerializer<BinaryValue>
    {
        public BinaryValue Read(BinaryReader binaryReader)
        {
            short len = binaryReader.ReadInt16();
            return new BinaryValue(new ArraySegment<byte>(binaryReader.ReadBytes(len), 0, len));
        }

        public BinaryValue Read(BinaryValue baseValue, BinaryReader binaryReader)
        {
            short len = binaryReader.ReadInt16();
            return new BinaryValue(new ArraySegment<byte>(binaryReader.ReadBytes(len), 0, len));
        }

        public void Write(BinaryValue value, BinaryWriter binaryWriter)
        {
            binaryWriter.Write((short) value.Buffer.Length);
            binaryWriter.Write(value.Buffer);
        }

        public void Write(BinaryValue baseValue, BinaryValue targetValue, BinaryWriter binaryWriter)
        {
            binaryWriter.Write((short) targetValue.Buffer.Length);
            binaryWriter.Write(targetValue.Buffer);
        }
    }
}