using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnsetDetection
{
    public class OnsetDetector
    {
        private List<float> _onsets;
        private DetectorOptions _options;
        private object _lock;

        public OnsetDetector(DetectorOptions options)
        {
            _onsets = new List<float>();
            _options = options;
            _lock = new object();
        }

        public List<float> Detect(string audioFile)
        {
            var audio = new Wav(audioFile);
            return Detect(audio);
        } 

        public List<float> Detect(Wav audio)
        {
            _onsets.Clear();
            _onsets = new List<float>();
            var onsets = new List<float>();

            //downmix the audio file
            audio.DownMix();

            //init detection specific variables
            int sliceSampleSize = (int)Math.Ceiling(_options.SliceLength * audio.Samplerate); //the size of each slice's sample
            int slicePaddingSize = (int)Math.Ceiling(_options.SlicePaddingLength * audio.Samplerate);
            int sliceCount = (int)Math.Ceiling((float)audio.Samples / sliceSampleSize); //the number of slices needed

            //init parallel specific variables
            var pOptions = new ParallelOptions();
            if (_options.MaxDegreeOfParallelism != -1) pOptions.MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism;
            ParallelLoopState loopState;

            List<Wav> wavSlices = new List<Wav>();
            for (int i = 0; i < sliceCount; i++)
            {
                int baseStart = i * sliceSampleSize;
                int adjustedStart = (baseStart - sliceSampleSize > 0) ? baseStart - slicePaddingSize : 0;
                int count = (sliceSampleSize + slicePaddingSize + baseStart > audio.Samples) ? audio.Samples - adjustedStart : sliceSampleSize + (baseStart - adjustedStart) + slicePaddingSize;
                float delay = (float)adjustedStart / audio.Samplerate;
                wavSlices.Add(new Wav(audio.Audio.SubMatrix(0, 1, adjustedStart, count), audio.Samplerate, count, 1) { Delay = delay });
            }
            var pLoopResult = Parallel.ForEach<Wav>(wavSlices, pOptions, (w, state) => GetOnsets(w));
            //while (!pLoopResult.IsCompleted) Thread.Sleep(1);
            if (!pLoopResult.IsCompleted) throw new Exception();

            onsets = _onsets.OrderBy(f => f).ToList();
            float prev = 0;
            float combine = 0.03f;
            List<float> ret = new List<float>();
            for (int i = 0; i < onsets.Count; i++)
            {
                if (onsets[i] - prev < combine)
                    continue;
                prev = onsets[i];
                ret.Add(onsets[i]);
            }
            return onsets;
        }

        private void GetOnsets(Wav w)
        {
            //construct the spectrogram
            var s = new Spectrogram(w, _options.WindowSize, _options.FPS, _options.Online, NeedPhaseInformation(_options.DetectionFunction));

            //construct the filterbank
            var filt = new Filter(_options.WindowSize / 2, w.Samplerate);

            //filter the spectrogram
            s.Filter(filt.Filterbank);

            //take the log of the spectrogram
            if (_options.Log) s.Log(_options.LogMultiplier, _options.LogAdd);

            //calculate the activations
            var sodf = new SpectralODF(s);
            var act = GetActivations(sodf, _options.DetectionFunction);

            //detect the onsets
            var o = new Onsets(act, _options.FPS);
            o.Detect(_options.ActivationThreshold, _options.MinimumTimeDelta,  delay: w.Delay * 1000);

            //add the onsets to the collection
            lock (_lock)
            {
                _onsets.AddRange(o.Detections);
            }
        }

        private Vector<float> GetActivations(SpectralODF sODF, Detectors detectionFunction)
        {
            Vector<float> activations;
            switch (detectionFunction)
            {
                case Detectors.HFC:
                    activations = sODF.HFC();
                    break;
                case Detectors.SD:
                    activations = sODF.SD();
                    break;
                case Detectors.SF:
                    activations = sODF.SF();
                    break;
                case Detectors.MKL:
                    activations = sODF.MKL();
                    break;
                case Detectors.PD:
                    activations = sODF.PD();
                    break;
                case Detectors.WPD:
                    activations = sODF.WPD();
                    break;
                case Detectors.NWPD:
                    activations = sODF.NWPD();
                    break;
                case Detectors.CD:
                    activations = sODF.CD();
                    break;
                case Detectors.RCD:
                    activations = sODF.RCD();
                    break;
                default:
                    throw new Exception("Unsupported detection function");
                    break;
            }

            return activations;
        }

        private bool NeedPhaseInformation(Detectors detectionFunction)
        {
            return new Detectors[] { Detectors.PD, Detectors.WPD, Detectors.NWPD, Detectors.CD, Detectors.RCD }.Contains(detectionFunction);
        }
    }

    public struct DetectorOptions
    {
        /// <summary>
        /// Slice the audio up into segments of this length for parallelism. Default is 10.0f <para />
        /// Given in seconds
        /// </summary>
        public float SliceLength;

        /// <summary>
        /// Padding to add to either end of a slice to ensure no beats are missed. Default is 0.01f <para /> 
        /// Given in seconds
        /// </summary>
        public float SlicePaddingLength;

        /// <summary>
        /// The max degree of parallelism to use. Default is -1 - scheduler decides
        /// </summary>
        public int MaxDegreeOfParallelism;

        /// <summary>
        /// The activation threshold to use for the detection. Default is 5f
        /// </summary>
        public float ActivationThreshold;

        /// <summary>
        /// The minimum time that must occur between successive onsets. Default is 30f <para />
        /// Given in milliseconds.
        /// </summary>
        public float MinimumTimeDelta;

        /// <summary>
        /// The size of the window in samples. Default is 2048
        /// </summary>
        public int WindowSize;

        /// <summary>
        /// The frames-per-second of the detector.  Default is 200
        /// </summary>
        public int FPS;

        /// <summary>
        /// Whether to use only past information or not. Default is true
        /// </summary>
        public bool Online;

        /// <summary>
        /// The onset detection function to use. Default is Detectors.SF
        /// </summary>
        public Detectors DetectionFunction;

        /// <summary>
        /// Whether to take the log of the spectrogram. Default is true
        /// </summary>
        public bool Log;

        /// <summary>
        /// Multiplier before taking the log. Default is 1
        /// </summary>
        public float LogMultiplier;

        /// <summary>
        /// Value added before taking the log. Default is 1
        /// </summary>
        public float LogAdd;

        public static DetectorOptions Default
        {
            get
            {
                return new DetectorOptions
                {
                    SliceLength = 10.0f,
                    SlicePaddingLength = 0.01f,
                    MaxDegreeOfParallelism = -1,
                    ActivationThreshold = 5f,
                    MinimumTimeDelta = 30f,
                    WindowSize = 2048,
                    FPS = 200,
                    Online = true,
                    DetectionFunction = Detectors.SF,
                    Log = true,
                    LogMultiplier = 1,
                    LogAdd = 1
                };
            }
        }

        //public static DetectorOptions Default()
        //{
        //    DetectorOptions options = new DetectorOptions();
        //    options.SliceLength = 10.0f;
        //    options.SlicePaddingLength = 0.01f;
        //    options.MaxDegreeOfParallelism = -1;
        //    options.ActivationThreshold = 5f;
        //    options.MinimumTimeDelta = 30f;
        //    options.WindowSize = 2048;
        //    options.FPS = 200;
        //    options.Online = true;
        //    options.DetectionFunction = Detectors.SF;
        //    options.Log = true;
        //    options.LogMultiplier = 1;
        //    options.LogAdd = 1;
        //    return options;
        //}
    }

    public enum Detectors
    {
        /// <summary>High Frequency Content</summary>
        HFC,
        /// <summary>Spectral Diff</summary>
        SD,
        /// <summary>Spectral Flux</summary>
        SF,
        /// <summary>Modified Kullback-Leibler</summary>
        MKL,
        /// <summary>Phase Deviation</summary>
        PD,
        /// <summary>Weighted Phase Deviation</summary>
        WPD,
        /// <summary>Normalized Weighted Phase Deviation</summary>
        NWPD,
        /// <summary>Complex Domain</summary>
        CD,
        /// <summary>Rectified Complex Domain</summary>
        RCD
    }
}
