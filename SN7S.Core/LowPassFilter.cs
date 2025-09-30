namespace SN7S.Core
{
    internal class LowPassFilter
    {
        private readonly float _alpha;
        private float _prev;

        internal LowPassFilter(float sampleRate, float cutoffHz)
        {
            float x = (float)Math.Exp(-2.0 * Math.PI * cutoffHz / sampleRate);
            _alpha = 1 - x;
        }

        internal float Process(float input)
        {
            _prev += _alpha * (input - _prev);
            return _prev;
        }
    }
}