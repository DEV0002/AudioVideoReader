using System;
using System.IO;
using System.Text;
using System.Collections;

using Toolbox;

namespace AudioReader {
    struct AudioFileFormat {
        public ushort nChannels;
        public uint SampleRate;
        public ushort BitsPerSample;
    }
    struct AudioFile {
        public AudioFileFormat fileFormat;
        public uint audioSize;
        public byte[] audioData;
    }
    abstract class AFR {
        public abstract AudioFile Read(string fileName);
        public abstract void Write(string fileName, AudioFile audio);
        public static Stream ConvertToWav(AudioFile audioFile) { //For Actually Playing the Audio
            byte[] wav = new byte[46 + audioFile.audioSize];
            Stream stream = new MemoryStream(wav);
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            stream.Write(BitConverter.GetBytes(38 + audioFile.audioSize), 0, 4);
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
            stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
            stream.Write(BitConverter.GetBytes(18), 0, 4);
            stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);
            stream.Write(BitConverter.GetBytes(audioFile.fileFormat.nChannels), 0, 2);
            stream.Write(BitConverter.GetBytes(audioFile.fileFormat.SampleRate), 0, 4);
            ushort nBlockAlign = (ushort)(audioFile.fileFormat.nChannels*(audioFile.fileFormat.BitsPerSample/8));
            stream.Write(BitConverter.GetBytes(audioFile.fileFormat.SampleRate * nBlockAlign), 0, 4);
            stream.Write(BitConverter.GetBytes(nBlockAlign), 0, 2);
            stream.Write(BitConverter.GetBytes((ushort)audioFile.fileFormat.BitsPerSample), 0, 2);
            stream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
            stream.Write(BitConverter.GetBytes(audioFile.audioSize), 0, 4);
            stream.Write(audioFile.audioData, 0, (int)audioFile.audioSize);
            stream.Flush();
            stream.Dispose();
            stream.Close();
            stream = new MemoryStream(wav);
            return stream;
        }
    }
    class Wav : AFR {
        public override AudioFile Read(string fileName) {
            AudioFile audioFile = new AudioFile();
            byte[] vs = File.ReadAllBytes(fileName+".wav");
            int index = 0;
            //Check File Validity
            if(ByteTools.BytesToString(vs, ref index, 4) != "RIFF") 
                throw new FileLoadException("File Header Invalid");
            int tFileLength = (int)ByteTools.BytesToUInt(vs, ref index);
            if(ByteTools.BytesToString(vs, ref index, 4) != "WAVE")
                throw new FileLoadException("File Header Invalid");
            if(ByteTools.BytesToString(vs, ref index, 4) != "fmt ")
                throw new FileLoadException("File Header Invalid");
            //Read Format
            AudioFileFormat format = new AudioFileFormat();
            int lengthOfHeader = (int)ByteTools.BytesToUInt(vs, ref index);
            int indexAfterHeader = index + lengthOfHeader;
            index += 2;
            format.nChannels = ByteTools.BytesToUShort(vs, ref index);
            format.SampleRate = ByteTools.BytesToUInt(vs, ref index);
            index += 6;
            format.BitsPerSample = ByteTools.BytesToUShort(vs, ref index);
            index = indexAfterHeader;
            //Check Validity Again
            while(ByteTools.BytesToString(vs, ref index, 4) != "data") {
                index -= 3;
                if(index > 100)
                    throw new FileLoadException("File Header Invalid");
            }
            //Read Audio Data
            audioFile.audioSize = ByteTools.BytesToUInt(vs, ref index);
            audioFile.audioData = new byte[(int)audioFile.audioSize];
            Array.Copy(vs, index, audioFile.audioData, 0, (int)audioFile.audioSize);
            //Return All Necessary Data
            audioFile.fileFormat = format;
            return audioFile;
        }
        public override void Write(string fileName, AudioFile audio) {
            
        }
    }
    class MP3 : AFR {
        bool async = false;
        int ID3Ver = 4;
        public override AudioFile Read(string fileName) {
            AudioFile audioFile = new AudioFile();
            byte[] vs = File.ReadAllBytes(fileName + ".mp3");
            int index = 0;
            readID3(vs, ref index);
            //Start Reading Frames
            while(index<vs.Length) {
                //Read Frame Header
                BitArray header = new BitArray(32);

                //TODO: Implement MP3 File Reading
            }
            return audioFile;
        }
        private void readID3(byte[] vs, ref int index) {
            //Read ID3 Header
            if(ByteTools.BytesToString(vs, ref index, 3) != "ID3")
                throw new FileLoadException("Invalid ID3");
            byte[] ver = new byte[2];
            Array.Copy(vs, index, ver, 0, 2);
            ID3Ver = ver[0];
            index += 2;
            BitArray flags = ByteTools.BytesToBitArray(vs, ref index, 1);
            uint size = ByteTools.BytesToSUInt(vs, ref index, true);
            //Set Important Flags
            async = flags.Get(0);
            if(ID3Ver == 0x02) {

            } else if(ID3Ver == 0x03) {
                bool ext = flags.Get(1);
            } else if(ID3Ver == 0x04) {
                bool ext = flags.Get(1);
                bool foot = flags.Get(3);
                bool upd = false;
                bool crc = false;
                ulong crcd = 0;
                bool rst = false;
                byte rstd = 0;
                if(ext) {
                    uint esize = ByteTools.BytesToSUInt(vs, ref index, true);
                    BitArray extFlags = ByteTools.BytesToBitArray(vs, ref index, 1);
                    upd = extFlags.Get(1);
                    crc = extFlags.Get(2);
                    if(upd)
                        index++;
                    if(crc) {
                        index++;
                        crcd = ByteTools.BytesToSULong35(vs, ref index, true);
                    }
                    if(rst) {
                        index++;
                        rstd = vs[index];
                        index++;
                        /*
                            rstd layout %ppqrrstt
                        p - Tag size restrictions
                            00   No more than 128 frames and 1 MB total tag size.
                            01   No more than 64 frames and 128 KB total tag size.
                            10   No more than 32 frames and 40 KB total tag size.
                            11   No more than 32 frames and 4 KB total tag size.
                        q - Text encoding restrictions
                            0    No restrictions
                            1    Strings are only encoded with ISO-8859-1 [ISO-8859-1] or UTF-8 [UTF-8].
                        r - Text fields size restrictions
                            00   No restrictions
                            01   No string is longer than 1024 characters.
                            10   No string is longer than 128 characters.
                            11   No string is longer than 30 characters.
                        s - Image encoding restrictions
                            0   No restrictions
                            1   Images are encoded only with PNG [PNG] or JPEG [JFIF].
                        t - Image size restrictions
                            00  No restrictions
                            01  All images are 256x256 pixels or smaller.
                            10  All images are 64x64 pixels or smaller.
                            11  All images are exactly 64x64 pixels, unless required otherwise.
                         */
                    }
                }

            } else
                throw new FileLoadException("ID3 Version Not Supported");
        }
        public override void Write(string fileName, AudioFile audio) {

        }
    }
}
