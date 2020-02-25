using System;
using System.Media;
using System.Diagnostics;
using AudioReader;

namespace AudioVideoReader {
    class Program {
        static void Main(string[] args) {
            AudioFile audioFile = new MP3().Read("C:\\Users\\DEV0002\\Music\\Chill\\I Can't Decide");
            SoundPlayer player = new SoundPlayer(AFR.ConvertToWav(audioFile));
            player.Load();
            long time = ((long)(audioFile.audioSize 
                / (audioFile.fileFormat.SampleRate * audioFile.fileFormat.nChannels
                * audioFile.fileFormat.BitsPerSample / 8))*1000)+1000;
            Stopwatch stopwatch = new Stopwatch();
            player.Play();
            stopwatch.Start();
            while(stopwatch.ElapsedMilliseconds < time)
                ;
        }
    }
}
