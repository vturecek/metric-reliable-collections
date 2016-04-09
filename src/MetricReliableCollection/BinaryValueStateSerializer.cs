// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;
    using System.IO;
    using Microsoft.ServiceFabric.Data;

    /// <summary>
    /// Serializer for BinaryValue.
    /// </summary>
    /// <remarks>
    /// Simple serializer for BinaryValue that writes the BinaryValue buffer directly to the 
    /// serialization stream prefixed by the length of the buffer.
    /// </remarks>
    internal class BinaryValueStateSerializer : IStateSerializer<BinaryValue>
    {
        /// <summary>
        /// Creates a BinaryValue from the binary data contained in the given BinaryReader.
        /// </summary>
        /// <remarks>
        /// See the write method remarks for format details.
        /// </remarks>
        /// <param name="binaryReader"></param>
        /// <returns></returns>
        public BinaryValue Read(BinaryReader binaryReader)
        {
            int len = 0;
            
            for (int i = 0, b = 0; i < 4 && (b = binaryReader.BaseStream.ReadByte()) >= 0; ++i)
            {
                len = (len | (b << 7 * i));

                if ((b & 128) == 0)
                {
                    break;
                }
            }

            return new BinaryValue(binaryReader.ReadBytes(len), false);
        }

        public BinaryValue Read(BinaryValue baseValue, BinaryReader binaryReader)
        {
            return this.Read(binaryReader);
        }

        /// <summary>
        /// Writes the given BinaryValue into the given BinaryWriter. See remarks for format.
        /// </summary>
        /// <remarks>
        /// The BinaryValue buffer length is written before its contents.
        /// The length is written using only the number of bytes required to represent the length.
        /// Each byte of the length value sets the most significant bit to indicate if 
        /// another byte should be read for the length value.
        /// When reading bytes to determine the length, the most significant bit of each byte
        /// should only be used to determine if another byte should be read
        /// to determine the buffer length and should be ommitted from the length value.
        /// </remarks>
        /// <param name="value"></param>
        /// <param name="binaryWriter"></param>
        public void Write(BinaryValue value, BinaryWriter binaryWriter)
        {
            int len = value.Buffer.Length;

            while (len != 0)
            {
                if (len > 127)
                {
                    binaryWriter.Write((byte)(len | 128));
                }
                else
                {
                    binaryWriter.Write((byte)(len & 127));
                }

                len = len >> 7;
            }

            binaryWriter.Write(value.Buffer);
        }

        public void Write(BinaryValue baseValue, BinaryValue targetValue, BinaryWriter binaryWriter)
        {
            this.Write(targetValue, binaryWriter);
        }
    }
}