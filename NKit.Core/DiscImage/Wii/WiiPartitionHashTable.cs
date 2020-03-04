using System;

namespace Nanook.NKit
{
    internal class PartitionHashTable
    {
        public byte[] Bytes { get; private set; }
        public int HashCount { get; private set; }

        internal PartitionHashTable(int hashCount)
        {
            Bytes = new byte[hashCount * 20]; //20 is sha1 length
            HashCount = hashCount;
        }
        public void Reset(byte[] group, int offset)
        {
            Array.Copy(group, offset, Bytes, 0, Math.Min(Bytes.Length, group.Length - offset));
        }
        public int CopyAll(byte[] buffer, int offset)
        {
            Array.Copy(Bytes, 0, buffer, offset, Math.Min(Bytes.Length, buffer.Length - offset));
            return Bytes.Length;
        }
        public bool Set(int blockIndex, byte[] sha1, bool testEqual)
        {
            if (testEqual && sha1.Equals(0, Bytes, blockIndex * 20, 20))
            {
                return true;
            }

            Array.Copy(sha1, 0, Bytes, blockIndex * 20, 20);
            return false;
        }
        public bool Equals(int blockIndex, byte[] sha1)
        {
            return sha1.Equals(0, Bytes, blockIndex * 20, 20);
        }
    }

}
