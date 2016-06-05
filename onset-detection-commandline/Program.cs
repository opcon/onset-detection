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
        static List<Onset> combinedOnsets;

        [STAThread]
        static void Main(string[] args)
        {
            TestRobustness(@"D:\Patrick\Documents\Development\Game Related\onset-detection\Test Song\One");

            //var of = new OpenFileDialog();
            //if (of.ShowDialog() != DialogResult.OK) return;
            //var file = of.FileName;
            //_lock = new object();
            //combinedOnsets = new List<Onset>();


            //TestSpeed(file);
            //var options = DetectorOptions.Default;
            //options.ActivationThreshold = 10;
            //var onsetDetector = new OnsetDetector(options, null);
            //var onsets = onsetDetector.Detect(file);

            //combinedOnsets = onsets;
            //GC.Collect(2, GCCollectionMode.Forced, true);
            //combinedOnsets = combinedOnsets.OrderBy(f => f.OnsetTime).ToList();
            //File.WriteAllLines("Strome - Papaoutai_onsets.csv", combinedOnsets.Select(f => f.ToString()).ToArray());
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void TestRobustness(string testFolder)
        {
            var options = DetectorOptions.Default;
            options.ActivationThreshold = 10;
            options.SliceLength = 10.0f;
            options.SlicePaddingLength = 0.5f;
            options.Online = false;
            var onsetDetector = new OnsetDetector(options, null);
            var files = Directory.GetFiles(testFolder).Where(f => ".mp3 .wav .flac".Contains(Path.GetExtension(f)));
            foreach (var f in files)
            {
                var ext = Path.GetExtension(f);
                var name = Path.GetFileNameWithoutExtension(f);
                var onsetName = name + "_" + ext + "_onsets.csv";
                var onsets = onsetDetector.Detect(f);
                File.WriteAllLines(Path.Combine(testFolder, onsetName), onsets.Select(s => s.ToString()).ToArray());
                Console.WriteLine("{0}: Sum - {1}, Average - {2}", name + "_" + ext, onsets.Sum(o => o.OnsetTime), onsets.Sum(o => o.OnsetTime) / onsets.Count);
                Console.WriteLine(onsets.Count);
            }

            //var allFiles = Directory.GetFiles(@"D:\Patrick\Music\My Music", "*.flac", SearchOption.AllDirectories);
            //int max = 100;
            //List<int> indicies = new List<int>();
            //List<string> testFiles = new List<string>();
            //var r = new Random();
            //for (int i = 0; i < max; i++)
            //{
            //    int index = r.Next(0, allFiles.Length);
            //    indicies.Add(index);
            //    testFiles.Add(allFiles[index]);
            //}
            //foreach (var f in testFiles)
            //{
            //    var onsets = onsetDetector.Detect(f);
            //    Console.WriteLine("{0}: Sum - {1}\tAverage - {2}", Path.GetFileNameWithoutExtension(f).PadRight(20).Substring(0,20), onsets.Sum(o => o.OnsetTime), onsets.Sum(o => o.OnsetTime) / onsets.Count);
            //}
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
