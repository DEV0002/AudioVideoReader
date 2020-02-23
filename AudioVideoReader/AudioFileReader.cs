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
        public abstract void Write(string fileName, AudioFileFormat format, byte[] data);
        public static Stream ConvertToWav(AudioFile audioFile) {
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
        public override void Write(string fileName, AudioFileFormat format, byte[] data) {
            
        }
    }
    class MP3 : AFR {
        public override AudioFile Read(string fileName) {
            AudioFile audioFile = new AudioFile();
            byte[] vs = File.ReadAllBytes(fileName + ".mp3");
            int index = 0;
            //Read ID3 Header
            byte[] ver = new byte[2];
            Array.Copy(vs, index+=2, ver, 0, 2);
            BitArray flags = ByteTools.BytesToBitArray(vs, ref index, 1);
            uint size = ByteTools.BytesToUInt(vs, ref index, true);
            //Set Important Flags
            bool async = flags.Get(0);
            bool ext = flags.Get(1);
            bool foot = flags.Get(3);
            //Start Reading Frames
            while(index<vs.Length) {
                //Read Frame Header
                BitArray header = new BitArray(32);

                //TODO: Implement MP3 File Reading
            }
            return audioFile;
        }
        public override void Write(string fileName, AudioFileFormat format, byte[] data) {

        }
    }
}
