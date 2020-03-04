﻿using System;
using System.Text;

namespace Nanook.NKit
{
    public enum SectionType { DiscHeader, PartitionHeader, Partition, Gap, End }

    public abstract class BaseSection
    {

        internal BaseSection(NStream stream, long discOffset, byte[] data, long size)
        {
            Stream = stream;
            DiscOffset = discOffset;
            Data = data;
            Size = size;
        }

        protected NStream Stream { get; private set; }
        public long DiscOffset { get; protected set; }
        public long Size { get; protected set; }
        public virtual byte[] Data { get; protected set; }

        public byte Read8(int offset) { return Data[offset]; }
        public ushort ReadUInt16B(int offset) { return bigEndian(BitConverter.ToUInt16(Data, offset)); }
        public uint ReadUInt32B(int offset) { return bigEndian(BitConverter.ToUInt32(Data, offset)); }
        public ulong ReadUInt64B(int offset) { return bigEndian(BitConverter.ToUInt64(Data, offset)); }
        public ushort ReadUInt16L(int offset) { return littleEndian(BitConverter.ToUInt16(Data, offset)); }
        public uint ReadUInt32L(int offset) { return littleEndian(BitConverter.ToUInt32(Data, offset)); }
        public ulong ReadUInt64L(int offset) { return littleEndian(BitConverter.ToUInt64(Data, offset)); }
        public string ReadString(int offset, int length) { return Encoding.ASCII.GetString(Data, offset, length); }
        public string ReadStringToNull(int offset, Encoding encoding) { return readStringToNull(encoding, offset, -1); }
        public string ReadStringToNull(int offset) { return readStringToNull(Encoding.ASCII, offset, -1); }
        public string ReadStringToNull(int offset, int maxLength) { return readStringToNull(Encoding.ASCII, offset, maxLength); }
        public byte[] Read(int offset, int length)
        {
            byte[] buffer = new byte[length];
            Array.Copy(Data, offset, buffer, 0, length);
            return buffer;
        }
        public void Write8(int offset, byte value) { Data[offset] = value; }
        public void WriteUInt16B(int offset, ushort value) { BitConverter.GetBytes(bigEndian(value)).CopyTo(Data, offset); }
        public void WriteUInt32B(int offset, uint value) { BitConverter.GetBytes(bigEndian(value)).CopyTo(Data, offset); }
        public void WriteUInt64B(int offset, ulong value) { BitConverter.GetBytes(bigEndian(value)).CopyTo(Data, offset); }
        public void WriteUInt16L(int offset, ushort value) { BitConverter.GetBytes(littleEndian(value)).CopyTo(Data, offset); }
        public void WriteUInt32L(int offset, uint value) { BitConverter.GetBytes(littleEndian(value)).CopyTo(Data, offset); }
        public void WriteUInt64L(int offset, ulong value) { BitConverter.GetBytes(littleEndian(value)).CopyTo(Data, offset); }
        public void WriteString(int offset, int length, string value) { Array.Copy(Encoding.ASCII.GetBytes(value), 0, Data, offset, length); }


        public void Write(int offset, byte[] buffer)
        {
            Array.Copy(buffer, 0, Data, offset, buffer.Length);
        }
        public void Write(int offset, byte[] buffer, int length)
        {
            Array.Copy(buffer, 0, Data, offset, length);
        }

        public void Write(int offset, byte[] buffer, int bufferOffset, int length)
        {
            Array.Copy(buffer, bufferOffset, Data, offset, length);
        }

        private uint bigEndian(uint x)
        {
            if (!BitConverter.IsLittleEndian) //don't swap on big endian CPUs
            {
                return x;
            }

            x = (x >> 16) | (x << 16);
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }
        private ulong bigEndian(ulong x)
        {
            if (!BitConverter.IsLittleEndian) //don't swap on big endian CPUs
            {
                return x;
            }

            return (ulong)(((bigEndian((uint)x) & 0xffffffffL) << 32) | (bigEndian((uint)(x >> 32)) & 0xffffffffL));
        }

        private uint littleEndian(uint x)
        {
            if (BitConverter.IsLittleEndian) //don't swap on big endian CPUs
            {
                return x;
            }

            x = (x >> 16) | (x << 16);
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        private ulong littleEndian(ulong x)
        {
            if (BitConverter.IsLittleEndian) //don't swap on big endian CPUs
            {
                return x;
            }

            return (ulong)(((littleEndian((uint)x) & 0xffffffffL) << 32) | (littleEndian((uint)(x >> 32)) & 0xffffffffL));
        }

        private ushort bigEndian(ushort x) { return !BitConverter.IsLittleEndian ? x : (ushort)((x >> 8) | (x << 8)); }
        private ushort littleEndian(ushort x) { return BitConverter.IsLittleEndian ? x : (ushort)((x >> 8) | (x << 8)); }

        private string readStringToNull(Encoding encoding, int offset, int maxLength)
        {
            try
            {
                byte b;
                int i = offset;
                int l = 0;

                while ((maxLength == -1 || l <= maxLength) && (b = Data[i++]) != '\0')
                {
                    l++;
                }

                return encoding.GetString(Data, offset, l);
            }
            catch (Exception ex)
            {
                throw new HandledException(ex, "NStream.readStringToNull failure");
            }
        }
    }
}
