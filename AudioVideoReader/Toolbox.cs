using System;
using System.Text;
using System.Collections;

namespace Toolbox {
    class ByteTools {
        public static string BytesToString(byte[] vs, ref int index, int length) {
            string text = Encoding.ASCII.GetString(vs,index,length);
            index += length;
            Console.WriteLine("index " + index + ": " + text);
            return text;
        }
        public static ushort BytesToUShort(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[2];
            Array.Copy(vs, index, bData, 0, 2);
            if(isBigEndian)
                Array.Reverse(bData);
            ushort data = BitConverter.ToUInt16(bData, 0);
            index += 2;
            Console.WriteLine("Index " + index + ": " + data);
            return data;
        }
        public static uint BytesToUInt(byte[] vs, ref int index, bool isBigEndian = false) {
            byte[] bData = new byte[4];
            Array.Copy(vs, index, bData, 0, 4);
            if(isBigEndian)
                Array.Reverse(bData);
            uint data = BitConverter.ToUInt32(bData, 0);
            index += 2;
            Console.WriteLine("Index " + index + ": " + data);
            return data;
        }
        public static BitArray BytesToBitArray(byte[] vs, ref int index, int length) {
            byte[] bData = new byte[length];
            Array.Copy(vs, index, bData, 0, length);
            BitArray bits = new BitArray(bData);
            index += length;
            Console.WriteLine(bits.ToString());
            return bits;
        }
    }
}
