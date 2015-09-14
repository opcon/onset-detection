using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            //var onsets = DetectOnsets(file);
            var onsetDetector = new OnsetDetector(DetectorOptions.Default);
            var onsets = onsetDetector.Detect(file);
            //List<Task> tasks = new List<Task>();
            //Console.WriteLine("Analyzing Onsets");
            //var baseWav = new Wav(@"D:\Patrick\Music\My Music\Chet Faker\Built On Glass\Chet Faker - Gold.flac");
            //baseWav.DownMix();
            //int sampleSize = MAXAUDIOSLICELENGTH * baseWav.Samplerate;
            //int sliceCount = (int)Math.Ceiling((float)baseWav.Samples / sampleSize);
            //for (int i = 0; i < sliceCount; i++)
            //{
            //    Wav w;
            //    int start = i * sampleSize;
            //    int count = (start + sampleSize > baseWav.Samples) ? baseWav.Samples - start : (sampleSize);
            //    float delay = (float)start / baseWav.Samplerate;
            //    w = new Wav(baseWav.Audio.SubMatrix(0, 1, start, count), baseWav.Samplerate, count, 1);
            //    tasks.Add(Task.Run(() => GetOnsets(w, delay)));
            //}
            //Task.WaitAll(tasks.ToArray());
            combinedOnsets = onsets;
            combinedOnsets = combinedOnsets.OrderBy(f => f).ToList();
            File.WriteAllLines("Chet Faker - Gold_onsets.csv", combinedOnsets.Select(f => f.ToString()).ToArray());
        }

        //private static void GetOnsets(Wav w)
        //{
        //    var s = new Spectrogram(w, 2048, 200, true, false);
        //    var filt = new Filter(2048 / 2, w.Samplerate);
        //    s.Filter(filt.Filterbank);
        //    s.Log(1, 1);
        //    var sodf = new SpectralODF(s);
        //    var act = sodf.SF();
        //    var o = new Onsets(act, 200);
        //    o.Detect(5f, delay: w.Delay * 1000);
        //    lock (_lock)
        //    {
        //        combinedOnsets.AddRange(o.Detections);
        //    }
        //}

        //static List<float> DetectOnsets(string audioFile)
        //{
        //    var onsets = new List<float>();

        //    //Load audio file
        //    var audio = new Wav(audioFile);

        //    //downmix the audio file
        //    audio.DownMix();

        //    //init detection specific variables
        //    int maxAudioSliceLength = 10; //the length of an audio slice in seconds
        //    float slicePaddingLength = 0.01f; //the padding of an audio slice in seconds;
        //    int sliceSampleSize = maxAudioSliceLength * audio.Samplerate; //the size of each slice's sample
        //    int slicePaddingSize = (int)Math.Ceiling(slicePaddingLength * audio.Samplerate);
        //    int sliceCount = (int)Math.Ceiling((float)audio.Samples / sliceSampleSize); //the number of slices needed

        //    //init parallel specific variables
        //    var options = new ParallelOptions();
        //    ParallelLoopState loopState;

        //    List<Wav> wavSlices = new List<Wav>();
        //    for (int i = 0; i < sliceCount; i++)
        //    {
        //        int baseStart = i * sliceSampleSize;
        //        int adjustedStart = (baseStart - sliceSampleSize > 0) ? baseStart - slicePaddingSize : 0;
        //        int count = (sliceSampleSize + slicePaddingSize + baseStart > audio.Samples) ? audio.Samples - adjustedStart : sliceSampleSize + (baseStart - adjustedStart) + slicePaddingSize;
        //        float delay = (float)adjustedStart / audio.Samplerate;
        //        wavSlices.Add(new Wav(audio.Audio.SubMatrix(0, 1, adjustedStart, count), audio.Samplerate, count, 1) { Delay = delay });
        //    }
        //    var pLoopResult = Parallel.ForEach<Wav>(wavSlices, options, (w, state) => GetOnsets(w));
        //    while (!pLoopResult.IsCompleted) Thread.Sleep(1);

        //    onsets = combinedOnsets.OrderBy(f => f).ToList();
        //    float prev = 0;
        //    float combine = 0.03f;
        //    List<float> ret = new List<float>();
        //    for (int i = 0; i < onsets.Count; i++)
        //    {
        //        if (onsets[i] - prev < combine)
        //            continue;
        //        prev = onsets[i];
        //        ret.Add(onsets[i]);
        //    }
        //    return onsets;
        //}
    }
}
