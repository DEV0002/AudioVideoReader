using System;
using System.Text;
using System.Collections;

using SInt28 = Toolbox.SynchsafeInt28;
using SInt35 = Toolbox.SynchsafeInt35;

namespace Toolbox {
    class ByteTools {
        public static string BytesToString(byte[] vs, ref int index, int length) {
            string text = Encoding.ASCII.GetString(vs,index,length);
            index += length;
            //Console.WriteLine("index " + index + ": " + text);
            return text;
        }
        public static string BytesToString(byte[] vs, ref int index, int length, Encoding encoding) {
            string text = encoding.GetString(vs, index, length);
            index += length;
            //Console.WriteLine("index " + index + ": " + text);
            return text;
        }
        public static ushort BytesToUShort(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[2];
            Array.Copy(vs, index, bData, 0, 2);
            if(isBigEndian)
                Array.Reverse(bData);
            ushort data = BitConverter.ToUInt16(bData, 0);
            index += 2;
            //Console.WriteLine("Index " + index + ": " + data);
            return data;
        }
        public static uint BytesToUInt(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[4];
            Array.Copy(vs, index, bData, 0, 4);
            if(isBigEndian)
                Array.Reverse(bData);
            uint data = BitConverter.ToUInt32(bData, 0);
            index += 4;
            //Console.WriteLine("Index " + index + ": " + data);
            return data;
        }
        public static SInt28 BytesToSInt28(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[4];
            Array.Copy(vs, index, bData, 0, 4);
            if(!isBigEndian)
                Array.Reverse(bData);
            SInt28 data = bData;
            index += 4;
            //Console.WriteLine("Index " + index + ": " + (uint)data);
            return data;
        }
        public static SInt35 BytesToSInt35(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[5];
            Array.Copy(vs, index, bData, 0, 5);
            if(!isBigEndian)
                Array.Reverse(bData);
            SInt35 data = bData;
            index += 5;
            //Console.WriteLine("Index " + index + ": " + (ulong)data);
            return data;
        }
        public static BitArray BytesToBitArray(byte[] vs, ref int index, int byteLength) {
            byte[] bData = new byte[byteLength];
            Array.Copy(vs, index, bData, 0, byteLength);
            BitArray bits = new BitArray(bData);
            index += byteLength;
            //Console.Write("Index " + index + ": ");
            //foreach(bool bit in bits) Console.Write(bit?"1":"0");
            //Console.WriteLine();
            return bits;
        }
    }
    class BitTools {
        public static int BitArrayToInt(BitArray bitArray, ref int index, int length) {
            if(index + length > bitArray.Length)
                throw new IndexOutOfRangeException();
            int value = 0;
            for(int i = index; i < index+length; i++)
                if(bitArray[i])
                    value += Convert.ToInt16(Math.Pow(2, i));
            index += length;
            return value;
        }
    }
    public struct SynchsafeInt28 {
        private UInt32 VALUE;
        public static implicit operator UInt32(SynchsafeInt28 value) {
            return value.VALUE;
        }
        public static implicit operator SynchsafeInt28(int value) {
            SynchsafeInt28 _ReturnValue = new SynchsafeInt28();
            _ReturnValue.VALUE = (uint)value;
            return _ReturnValue;
        }
        public static SynchsafeInt28 operator -(SynchsafeInt28 value, int value1) {
            SynchsafeInt28 _ReturnValue = new SynchsafeInt28();
            _ReturnValue.VALUE = value.VALUE - (uint)value1;
            return _ReturnValue;
        }
        public static implicit operator SynchsafeInt28(byte[] value) {
            SynchsafeInt28 _ReturnValue = new SynchsafeInt28();
            UInt32 byte0 = value[0];
            UInt32 byte1 = value[1];
            UInt32 byte2 = value[2];
            UInt32 byte3 = value[3];
            _ReturnValue.VALUE = byte0 << 21 | byte1 << 14 | byte2 << 7 | byte3;
            return _ReturnValue;
        }
    }
    public struct SynchsafeInt35 {
        private UInt64 VALUE;
        public static implicit operator UInt64(SynchsafeInt35 value) {
            return value.VALUE;
        }
        public static implicit operator SynchsafeInt35(int value) {
            SynchsafeInt35 _ReturnValue = new SynchsafeInt35();
            _ReturnValue.VALUE = (uint)value;
            return _ReturnValue;
        }
        public static SynchsafeInt35 operator -(SynchsafeInt35 value, int value1) {
            SynchsafeInt35 _ReturnValue = new SynchsafeInt35();
            _ReturnValue.VALUE = value.VALUE - (uint)value1;
            return _ReturnValue;
        }
        public static implicit operator SynchsafeInt35(byte[] value) {
            SynchsafeInt35 _ReturnValue = new SynchsafeInt35();
            UInt64 byte0 = value[0];
            UInt64 byte1 = value[1];
            UInt64 byte2 = value[2];
            UInt64 byte3 = value[3];
            UInt64 byte4 = value[4];
            _ReturnValue.VALUE = byte0 << 28 | byte1 << 21 | byte2 << 14 | byte3 << 7 | byte4;
            return _ReturnValue;
        }
    }
}
