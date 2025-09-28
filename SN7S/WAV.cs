using System.Text;

namespace SN7S
{
    public class WAVWriter(int sampleRate = 44100, int numChannels = 1, short bitsPerSample = 16)
    {
        private readonly int _sampleRate = sampleRate;
        private readonly int _numChannels = numChannels;
        private readonly short _bitsPerSample = bitsPerSample;

        public void Write(string filePath, List<short> samples)
        {
            using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write);
            using BinaryWriter bw = new(fs, Encoding.ASCII);

            int byteRate = _sampleRate * _numChannels * (_bitsPerSample / 8);
            short blockAlign = (short)(_numChannels * (_bitsPerSample / 8));
            int subchunk2Size = samples.Count * (_bitsPerSample / 8);
            int chunkSize = 36 + subchunk2Size;

            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(chunkSize);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));

            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)_numChannels);
            bw.Write(_sampleRate);
            bw.Write(byteRate);
            bw.Write(blockAlign);
            bw.Write(_bitsPerSample);

            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(subchunk2Size);

            foreach (short s in samples)
                bw.Write(s);
        }
    }
}