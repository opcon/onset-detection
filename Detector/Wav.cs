using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSCore;
using CSCore.Codecs;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace OnsetDetection
{
    /// <summary>
    /// Wav Class is a simple wrapper around cscore
    /// </summary>
    public class Wav
    {
        public int Samplerate;
        public int Samples;
        public int Channels;
        public Matrix<float> Audio;

        /// <summary>
        /// Creates a new Wav object instance of the given file
        /// </summary>
        /// <param name="filename">name of the .wav file</param>
        public Wav(string filename)
        {
            //read in the audio
            var ss = CodecFactory.Instance.GetCodec(filename).ToSampleSource();
            Samplerate = ss.WaveFormat.SampleRate;
            Channels = ss.WaveFormat.Channels;
            Samples = (int)ss.Length / Channels;
            float[] buffer = new float[ss.Length];
            ss.Read(buffer, 0, (int)ss.Length);

            //load the channel data
            Audio = DenseMatrix.Create(buffer.Length / Channels, Channels, 0);
            for (int i = 0; i < Audio.RowCount; i++)
            {
                for (int j = 0; j < Audio.ColumnCount; j++)
                {
                    Audio[i, j] = buffer[i * Channels + j];
                }
            }
        }

        public Wav(Matrix<float> audio, int samplerate, int samples, int channels)
        {
            Audio = audio;
            Samplerate = samplerate;
            Samples = samples;
            Channels = channels;
        }

        /// <summary>
        /// Attenuate the audio signal
        /// </summary>
        /// <param name="attenuation">attenuation level given in dB</param>
        public void Attenuate(float attenuation)
        {
            Audio = Audio.Divide((float)Math.Pow(Math.Sqrt(10), attenuation / 10));
        }

        /// <summary>
        /// Down-mix the signal to mono
        /// </summary>
        public void DownMix()
        {
            if (Channels > 1)
                Audio = Matrix<float>.Build.DenseOfRowVectors(Audio.RowSums().Divide(Channels));
        }

        /// <summary>
        /// Normalize the audio signal
        /// </summary>
        public void Normalize()
        {
            Audio = Audio.Divide(Audio.ToArray().Cast<float>().Max());
        }
    }
}