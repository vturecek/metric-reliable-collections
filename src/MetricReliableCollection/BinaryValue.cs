// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MetricReliableCollections
{
    using System;

    internal struct BinaryValue : IComparable<BinaryValue>, IEquatable<BinaryValue>
    {
        internal byte[] Buffer { get; }

        public BinaryValue(ArraySegment<byte> buffer)
        {
            this.Buffer = new byte[buffer.Count];

            Array.Copy(buffer.Array, buffer.Offset, this.Buffer, 0, buffer.Count);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BinaryValue))
            {
                return false;
            }

            return this.Equals((BinaryValue) obj);
        }

        /// <summary>
        /// There will be collisions. Needs improvement!
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (this.Buffer != null)
            {
                int sum = this.Buffer.Length;
                for (int i = 0; i < this.Buffer.Length; ++i)
                {
                    sum += this.Buffer[i];
                }

                return sum;
            }

            return 0;
        }

        public int CompareTo(BinaryValue other)
        {
            if (this.Buffer == null && other.Buffer == null)
            {
                return 0;
            }

            if (this.Buffer == null)
            {
                return -1;
            }

            if (other.Buffer == null)
            {
                return 1;
            }

            int len = Math.Min(this.Buffer.Length, other.Buffer.Length);

            for (int i = 0; i < len; ++i)
            {
                int c = this.Buffer[i].CompareTo(other.Buffer[i]);

                if (c != 0)
                {
                    return c;
                }
            }

            return this.Buffer.Length.CompareTo(other.Buffer.Length);
        }

        public bool Equals(BinaryValue other)
        {
            if (this.Buffer == null && other.Buffer == null)
            {
                return true;
            }

            if (this.Buffer == null ^ other.Buffer == null)
            {
                return false;
            }

            int len = Math.Min(this.Buffer.Length, other.Buffer.Length);

            for (int i = 0; i < len; ++i)
            {
                if (!this.Buffer[i].Equals(other.Buffer[i]))
                {
                    return false;
                }
            }

            return this.Buffer.Length.Equals(other.Buffer.Length);
        }
    }
}