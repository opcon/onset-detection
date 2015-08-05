using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnsetDetection
{
    class Program
    {
        const int MAXAUDIOSLICELENGTH = 10; //length of audio slice in seconds
        static object _lock;
        static List<float> combinedOnsets;
        static void Main(string[] args)
        {
            List<Task> tasks = new List<Task>();
            _lock = new object();
            combinedOnsets = new List<float>();
            Console.WriteLine("Analyzing Onsets");
            //var w = new Wav(@"D:\Patrick\Desktop\godl.wav");
            var baseWav = new Wav(@"D:\Patrick\Music\My Music\Chet Faker\Built On Glass\Chet Faker - Gold.flac");
            baseWav.DownMix();
            int sampleSize = MAXAUDIOSLICELENGTH * baseWav.Samplerate;
            int sliceCount = (int)Math.Ceiling((float)baseWav.Samples / sampleSize);
            for (int i = 0; i < sliceCount; i++)
            {
                Wav w;
                int start = i * sampleSize;
                int count = (start + sampleSize > baseWav.Samples) ? baseWav.Samples - start : (sampleSize);
                float delay = (float)start / baseWav.Samplerate;
                w = new Wav(baseWav.Audio.SubMatrix(0, 1, start, count), baseWav.Samplerate, count, 1);
                tasks.Add(Task.Run(() => GetOnsets(w, delay)));
            }
            Task.WaitAll(tasks.ToArray());
            combinedOnsets = combinedOnsets.OrderBy(f => f).ToList();
            File.WriteAllLines("Chet Faker - Gold_onsets.csv", combinedOnsets.Select(f => f.ToString()).ToArray());
        }

        private static void GetOnsets(Wav w, float delay)
        {
            var s = new Spectrogram(w, 2048, 200, true, false);
            var filt = new Filter(2048 / 2, w.Samplerate);
            s.Filter(filt.Filterbank);
            s.Log(1, 1);
            var sodf = new SpectralODF(s);
            var act = sodf.SF();
            var o = new Onsets(act, 200);
            o.Detect(10f, delay: delay * 1000);
            lock (_lock)
            {
                combinedOnsets.AddRange(o.Detections);
            }
        }
    }
}
