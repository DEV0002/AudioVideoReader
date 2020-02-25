using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Toolbox;
using SInt28 = Toolbox.SynchsafeInt28;
using SInt35 = Toolbox.SynchsafeInt35;

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
    class MP3 {
        bool async = false;
        float ID3Ver = 4;
        ArrayList frames;
        uint[][] bitrateIndex = {
            new uint[] { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0 },
            new uint[] { 0, 32, 48, 56, 64,  80,  96,  112, 128, 160, 192, 224, 256, 320, 384, 0 },
            new uint[] { 0, 32, 40, 48, 56,  64,  80,  96,  112, 128, 160, 192, 224, 256, 320, 0 },
            new uint[] { 0, 32, 48, 56, 64,  80,  96,  112, 128, 144, 160, 176, 192, 224, 256, 0 },
            new uint[] { 0, 8,  16, 24, 32,  40,  48,  56,  64,  80,  96,  112, 128, 144, 160, 0 }
        };
        uint[][] samplerateIndex = {
            new uint[] { 44100, 48000, 32000, 0 },
            new uint[] { 22050, 24000, 16000, 0 },
            new uint[] { 11025, 12000, 8000, 0 }
        };
        struct ID3Frames {
            public string ID;
            public SInt28 frameSize;
            public BitArray frameFlags;
            public string disenc;
            public string txt;
        }
        struct MP3FrameHeader {
            public int MPEGId;
            public int MPEGLayr;
            public bool prot;
            public uint bitrate;
            public uint sampleRate;
            public bool pad;
            public int channelMode;
            public bool IS;
            public bool MSS;
            public int emph;
            public int bps;
            public int channels;
        }
        public AudioFile Read(string fileName, out ArrayList ID3Frames) {
            AudioFile audioFile = new AudioFile();
            byte[] vs = File.ReadAllBytes(fileName + ".mp3");
            int index = 0;
            //Load ID3
            readID3(vs, ref index);
            Console.WriteLine("ID3v2."+ID3Ver+" Header Loaded");
            //Start Reading Frames
            AudioFileFormat format = new AudioFileFormat();
            byte[] audioData;
            while(index<vs.Length) {
                //TODO: Implement MP3 File Reading
                //Read Frame Header
                byte[] headerBytes = new byte[4];
                Array.Copy(vs, index, headerBytes, 0, 4);
                index += 4;
                MP3FrameHeader header = ReadFrameHeader(headerBytes);
                if(format.BitsPerSample != header.bps)
                    format.BitsPerSample = (ushort)header.bps;
                if(format.SampleRate != header.sampleRate)
                    format.SampleRate = header.sampleRate;
                if(format.nChannels != header.channels)
                    format.nChannels = (ushort)header.channels;
                if(header.prot)
                    index += 2;
                int bound = header.channelMode == 0b0 ? 4 : header.channelMode == 0b01 ? 8 : header.channelMode == 0b10 ? 12 : header.channelMode == 0b11 ? 16 : -1;
            }
            ID3Frames = frames;
            return audioFile;
        }

        private MP3FrameHeader ReadFrameHeader(byte[] headerBytes) {
            MP3FrameHeader frameHeader = new MP3FrameHeader();
            BitArray header = new BitArray(headerBytes);
            int headerIndex = 11;
            int MPEGId = BitTools.BitArrayToInt(header, ref headerIndex, 2);
            if(MPEGId == 0) {
                throw new FileLoadException("MPEG-2.5 Not Supported");
            } else if(MPEGId == 2) {
                throw new FileLoadException("MPEG-2 Not Supported");
            } else if(MPEGId != 3)
                throw new FileLoadException("Invalid Version ID");
            frameHeader.MPEGId = MPEGId;
            int MPEGLayr = BitTools.BitArrayToInt(header, ref headerIndex, 2);
            //0 = res, 1 = Layer 3, 2 = Layer 2, 3 = Layer 1
            frameHeader.MPEGLayr = MPEGLayr;
            frameHeader.prot = header.Get(headerIndex++);
            uint bitrate = 0;
            if(MPEGId == 2) {
                bitrate = bitrateIndex[3 - MPEGLayr][BitTools.BitArrayToInt(header, ref headerIndex, 4)];
            } else if(MPEGId == 2 || MPEGId == 0) {
                if(MPEGLayr == 1)
                    bitrate = bitrateIndex[4][BitTools.BitArrayToInt(header, ref headerIndex, 4)];
                else
                    bitrate = bitrateIndex[5][BitTools.BitArrayToInt(header, ref headerIndex, 4)];
            }
            bitrate *= 1000;
            frameHeader.bitrate = bitrate;
            uint sampleRate = 0;
            switch(MPEGId) {
                case 0:
                    sampleRate = samplerateIndex[3][BitTools.BitArrayToInt(header, ref headerIndex, 2)];
                    break;
                case 2:
                    sampleRate = samplerateIndex[0][BitTools.BitArrayToInt(header, ref headerIndex, 2)];
                    break;
                case 3:
                    sampleRate = samplerateIndex[1][BitTools.BitArrayToInt(header, ref headerIndex, 2)];
                    break;
            }
            frameHeader.sampleRate = sampleRate;
            frameHeader.pad = header.Get(headerIndex++);
            headerIndex++;
            int channelMode = BitTools.BitArrayToInt(header, ref headerIndex, 2);
            if(channelMode == 1) {
                frameHeader.MSS = header.Get(headerIndex++);
                frameHeader.IS = header.Get(headerIndex++);
            } else
                headerIndex += 2;
            frameHeader.channelMode = channelMode;
            headerIndex += 2;
            frameHeader.emph = BitTools.BitArrayToInt(header, ref headerIndex, 2);
            int bitsPerSample = 0;
            if(channelMode == 1 || channelMode == 3)
                bitsPerSample = (int)(bitrate / (sampleRate * 2));
            else if(channelMode == 0 || channelMode == 2)
                bitsPerSample = (int)(bitrate / sampleRate);
            frameHeader.bps = bitsPerSample;
            if(channelMode == 0b11)
                frameHeader.channels = 1;
            else
                frameHeader.channels = 2;
            return frameHeader;
        }

        private void readID3(byte[] vs, ref int index) {
            //Read ID3 Header
            if(ByteTools.BytesToString(vs, ref index, 3) != "ID3")
                throw new FileLoadException("Invalid ID3");
            byte[] ver = new byte[2];
            Array.Copy(vs, index, ver, 0, 2);
            ID3Ver = (float)ver[0]+((float)ver[1]/10.0f);
            index += 2;
            BitArray flags = ByteTools.BytesToBitArray(vs, ref index, 1);
            SInt28 size = ByteTools.BytesToSInt28(vs, ref index, true);
            //Set Important Flags
            async = flags.Get(0);
            if(ver[0] == 0x00) {

            } else if(ver[0] == 0x00) {
                bool ext = flags.Get(1);
            } else if(ver[0] == 0x04) {
                bool ext = flags.Get(1);
                bool foot = flags.Get(3);
                bool upd = false;
                bool crc = false;
                SInt35 crcd = 0;
                bool rst = false;
                byte rstd = 0;
                if(ext) {
                    SInt28 esize = ByteTools.BytesToSInt28(vs, ref index, true);
                    BitArray extFlags = ByteTools.BytesToBitArray(vs, ref index, 1);
                    upd = extFlags.Get(1);
                    crc = extFlags.Get(2);
                    rst = extFlags.Get(3);
                    if(upd)
                        index++;
                    if(crc) {
                        index++;
                        crcd = ByteTools.BytesToSInt35(vs, ref index, true);
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
                throw new FileLoadException("ID3v2."+ID3Ver+" Not Supported");
            if(ver[0] == 0x03 || ver[0] == 0x04) {
                frames = new ArrayList();
                while(vs[index] != 0xFF && (vs[index + 1] & 0b1110) != 0) {
                    //Read Frame Header
                    ID3Frames frame = new ID3Frames();
                    frame.ID = ByteTools.BytesToString(vs, ref index, 4);
                    frame.frameSize = ByteTools.BytesToSInt28(vs, ref index, true);
                    frame.frameFlags = ByteTools.BytesToBitArray(vs, ref index, 2);
                    //Read Frame Data
                    byte encoding = vs[index++];
                    frame.frameSize = frame.frameSize - 1;
                    switch(encoding) {
                        case 0:
                            frame.disenc = "ISO-8859-1";
                            frame.txt = ByteTools.BytesToString(vs, ref index, (int)(uint)frame.frameSize, Encoding.GetEncoding("ISO-8859-1"));
                            break;
                        case 1:
                            frame.disenc = "UTF-16";
                            frame.txt = ByteTools.BytesToString(vs, ref index, (int)(uint)frame.frameSize, Encoding.Unicode);
                            break;
                        case 2:
                            frame.disenc = "UTF-16BE";
                            frame.txt = ByteTools.BytesToString(vs, ref index, (int)(uint)frame.frameSize, Encoding.BigEndianUnicode);
                            break;
                        case 3:
                            frame.disenc = "UTF-8";
                            frame.txt = ByteTools.BytesToString(vs, ref index, (int)(uint)frame.frameSize, Encoding.UTF8);
                            break;
                    }
                    //Add Data to Frame Collection
                    frames.Add(frame);
                    //Report Data Collected
                    Console.WriteLine("Frame ID: " + frame.ID);
                    Console.WriteLine("Frame Size: " + (uint)frame.frameSize);
                    Console.Write("Flags: ");
                    foreach(bool bit in frame.frameFlags)
                        Console.Write(bit ? "1" : "0");
                    Console.WriteLine("\nText Encoding Type: " + frame.disenc);
                    Console.WriteLine("Text Data: " + frame.txt + "\n");
                }
            }
        }
        public void Write(string fileName, AudioFile audio) {

        }
    }
}
