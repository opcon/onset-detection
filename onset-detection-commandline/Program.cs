using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnsetDetection
{
    class Program
    {
        static object _lock;
        static List<float> combinedOnsets;

        [STAThread]
        static void Main(string[] args)
        {
            var of = new OpenFileDialog();
            if (of.ShowDialog() != DialogResult.OK) return;
            var file = of.FileName;
            _lock = new object();
            combinedOnsets = new List<float>();

            //TestSpeed(file);
            var options = DetectorOptions.Default;
            options.ActivationThreshold = 10;
            var onsetDetector = new OnsetDetector(options, null);
            var onsets = onsetDetector.Detect(file);

            combinedOnsets = onsets;
            GC.Collect(2, GCCollectionMode.Forced, true);
            combinedOnsets = combinedOnsets.OrderBy(f => f).ToList();
            File.WriteAllLines("Strome - Papaoutai_onsets.csv", combinedOnsets.Select(f => f.ToString()).ToArray());
        }

        public static void TestSpeed(string file)
        {
            Dictionary<float, float> Times = new Dictionary<float, float>();
            OnsetDetector onsetDetector;
            Stopwatch watch;
            int warmup = 5;
            int repeats = 3;
            float startSliceLength = 0.5f; //500 milliseconds
            float endSliceLength = 18; //5 seconds
            float step = 0.5f;

            //warmup
            for (int i = 0; i < warmup; i++)
            {
                Console.WriteLine("Warming up, number {0} out of {1}", i+1, warmup);
                onsetDetector = new OnsetDetector(DetectorOptions.Default, null);
                onsetDetector.Detect(file);
            }

            //trials
            for (float slice = startSliceLength; slice < endSliceLength; slice += step) 
            {
                Console.WriteLine("Using slice length {0}", slice);
                var options = DetectorOptions.Default;
                options.SliceLength = slice;
                float time = 0.0f;
                watch = new Stopwatch();

                for (int k = 0; k < repeats; k++)
                {
                    Console.WriteLine("Beginning trial {0}", k);
                    watch.Restart();
                    onsetDetector = new OnsetDetector(options, null);
                    onsetDetector.Detect(file);
                    watch.Stop();
                    time += watch.ElapsedMilliseconds;
                }

                time /= repeats;
                Times.Add(slice, time);
            }
            foreach (var time in Times)
            {
                Console.WriteLine("Slice Length (s):{0}\tTime Taken:{1} ms, {2} s", time.Key, time.Value, time.Value / 1000);
            }
            Console.ReadLine();
        }
    }
}
