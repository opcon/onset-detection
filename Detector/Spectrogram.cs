using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex; 

namespace OnsetDetection
{
    /// <summary>
    /// Spectrogram Class
    /// </summary>
    public class Spectrogram
    {
        Wav _wav;
        int _fps;
        public float HopSize;
        int _frames;
        int _ffts;
        public int Bins;
        Matrix<System.Numerics.Complex> _STFT;
        public Matrix<float> Phase;
        public Matrix<float> Spec;
        public Vector<float> Window;

        /// <summary>
        /// Creates a new Spectrogram object instance and performs a STFT on the given audio
        /// </summary>
        /// <param name="wav">a Wav object</param>
        /// <param name="windowSize">is the size for the window in samples</param>
        /// <param name="fps">is the desired frame rate</param>
        /// <param name="online">work in online mode (i.e. use only past audio information)</param>
        /// <param name="phase">include phase information</param>
        public Spectrogram(Wav wav, int windowSize=2048, int fps=200, bool online=true, bool phase=true)
        {
            //init some variables
            _wav = wav;
            _fps = fps;
            //derive some variables
            HopSize = _wav.Samplerate / (float)_fps; //use floats so that seeking works properly
            _frames = (int)(_wav.Samples / HopSize);
            _ffts = windowSize / 2;
            Bins = windowSize / 2; //initial number equal to ffts, can change if filters are used
            //init STFT matrix
            _STFT = DenseMatrix.Create(_frames, _ffts, System.Numerics.Complex.Zero);
            //create windowing function
            var cArray = wav.Audio.ToRowArrays()[0];
            Window = Vector<float>.Build.DenseOfArray(MathNet.Numerics.Window.Hann(windowSize).Select(d => (float)d).ToArray());
            //step through all frames
            foreach (var frame in Enumerable.Range(0, _frames))
            {
                int seek;
                Vector<float> signal;
                //seek to the right position in the audio signal
                if (online)
                    //step back a complete windowSize after moving forward 1 hopSize
                    //so that the current position is at the stop of the window
                    seek = (int)((frame + 1) * HopSize - windowSize);
                else
                    //step back half of the windowSize so that the frame represents the centre of the window
                    seek = (int)(frame * HopSize - windowSize / 2);
                //read in the right portion of the audio
                if (seek >= _wav.Samples)
                    //stop of file reached
                    break;
                else if (seek + windowSize > _wav.Samples)
                {
                    //stop behind the actual audio stop, append zeros accordingly
                    var zeros = Vector<float>.Build.Dense(seek + windowSize - _wav.Samples, 0);
                    var t = PythonUtilities.Slice<float>(cArray, seek, cArray.Length).ToList();
                    t.AddRange(zeros.ToList());
                    signal = Vector<float>.Build.DenseOfEnumerable(t);
                }
                else if (seek < 0)
                {
                    //start before actual audio start, pad with zeros accordingly
                    var zeros = Vector<float>.Build.Dense(-seek, 0).ToList();
                    var t = PythonUtilities.Slice<float>(cArray, 0, seek + windowSize).ToList();
                    zeros.AddRange(t);
                    signal = Vector<float>.Build.DenseOfEnumerable(zeros);
                }
                else
                {
                    //normal read operation
                    signal = Vector<float>.Build.DenseOfEnumerable(PythonUtilities.Slice<float>(cArray, seek, seek + windowSize));
                }
                //multiply the signal with the window function
                signal = signal.PointwiseMultiply(Window);
                //only shift and perform complex DFT if needed
                if (phase)
                {
                    //circular shift the signal (needed for correct phase)
                    signal = NumpyCompatibility.FFTShift(signal);
                }
                //perform DFT
                var result = signal.Map(f => (System.Numerics.Complex)f).ToArray();
                MathNet.Numerics.IntegralTransforms.Fourier.BluesteinForward(result, MathNet.Numerics.IntegralTransforms.FourierOptions.NoScaling);
                _STFT.SetRow(frame, result.Take(_ffts).ToArray());
                //next frame
            }
            //magnitude spectrogram
            Spec = _STFT.Map(c => (float)c.Magnitude);
            //phase
            if (phase)
            {
                var imag = _STFT.Map(c => (float)c.Imaginary);
                var real = _STFT.Map(c => (float)c.Real);
                Phase = real.Map2((r, i) => (float)Math.Atan2(i,r), imag);
            }
        }

        /// <summary>
        /// Perform adaptive whitening on the magnitude spectrogram
        /// </summary>
        /// <param name="floor">floor value</param>
        /// <param name="relaxation">relaxation time in seconds</param>
        /// "Adaptive Whitening For Improved Real-time Audio Onset Detection"
        /// Dan Stowell and Mark Plumbley
        /// Proceedings of the International Computer Music Conference(ICMC), 2007
        public void AW(int floor=5, int relaxation=10)
        {
            var memCoeff = (float)Math.Pow(10.0, (-6 * relaxation / _fps));
            var P = Matrix<float>.Build.SameAs(Spec);
            //iterate over all frames
            foreach (var f in Enumerable.Range(0, _frames))
            {
                Vector<float> spec_floor = Vector<float>.Build.Dense(Spec.ColumnCount);
                for (int i = 0; i < Spec.ColumnCount; i++)
                {
                    spec_floor[i] = Math.Max(Spec[f, i], floor);
                }
                //var spec_floor = Math.Max(Spec.ToRowArrays()[f].ToList().Max(), floor);
                if (f > 0)
                    for (int i = 0; i < P.ColumnCount; i++)
                    {
                        P[f, i] = Math.Max(spec_floor[i], memCoeff * P[f - 1, i]);
                    }
                else
                    P.SetRow(f, spec_floor);
            }
            //adjust spec
            Spec = Spec.PointwiseDivide(P);
        }

        /// <summary>
        /// Filter the magnitude spectrogram with a filterbank
        /// If no filter is given a standard one will be created
        /// </summary>
        /// <param name="Filterbank">Filter object which includes the filterbank</param>
        public void Filter(Matrix<float> filterbank = null)
        {
            if (filterbank == null)
                //construct a standard filterbank
                filterbank = new Filter(_ffts, _wav.Samplerate).Filterbank;
            //filter the magnitude spectrogram with the filterbank
            Spec = Spec.Multiply(filterbank);
            //adjust the number of bins
            Bins = Spec.ColumnCount;
        }

        /// <summary>
        /// Take the logarithm of the magnitude spectrogram
        /// </summary>
        /// <param name="mul">multiply the magnitude spectrogram with given value</param>
        /// <param name="add">add the given value to the magnitude spectrogram</param>
        public void Log(int mul=20, int add=1)
        {
            if (add <= 0) throw new Exception("a positive value must be added before taking the logarithm");
            Spec = Spec.Map(f => (float)Math.Log10(mul * f + add), Zeros.Include);
        }
    }
}