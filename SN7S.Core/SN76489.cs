using System.Runtime.CompilerServices;

namespace SN7S.Core
{
    public sealed class SN76489
    {
        private readonly float[] _volumeTable = new float[16];

        private readonly ToneChannel[] _tones = [ new(1), new(2), new(3) ];
        private readonly NoiseChannel _noise = new();

        private readonly int _lsfrShift;
        private readonly Func<int> _whiteNoiseFeedback;

        private readonly CommandQueue _commands = new();
        private LatchCommand _latch = new();

        private int _prescaler = 16;
        private ulong _cycleCount;
        private ulong _cycleFrac;

        private readonly ulong _clockRate;
        private readonly ulong _sampleRate;

        public SN76489(uint clockRate, uint sampleRate, uint lsfrSize)
        {
            _clockRate = clockRate;
            _sampleRate = sampleRate;

            for (int i = 0; i < 15; i++)
                _volumeTable[i] = (float)Math.Pow(10.0, (-2.0 * i) / 20.0);

            _volumeTable[15] = 0.0f;

            if (lsfrSize != 15 && lsfrSize != 16)
                Console.WriteLine("WARNING: Invalid LSFR size");

            _whiteNoiseFeedback = lsfrSize switch
            {
                15 => () => ((_noise.LSFR >> 0) ^ (_noise.LSFR >> 1)) & 1, // TI SN76489
                16 => () => ((_noise.LSFR >> 0) ^ (_noise.LSFR >> 3)) & 1, // Sega clone
                _  => () => 0,
            };

            _lsfrShift = (int)(lsfrSize - 1);

            Reset();
        }


        public void GenerateSamples(Span<short> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = GenerateSample();
        }

        public short GenerateSample()
        {
            ulong targetCycle = _cycleCount + GetCyclesForSample();

            while (_commands.Count > 0)
            {
                var next = _commands.Peek();
                if (next.Cycle > targetCycle) break;

                Tick(next.Cycle - _cycleCount);
                _cycleCount = next.Cycle;

                ApplyWrite(_commands.Dequeue());
            }

            Tick(targetCycle - _cycleCount);
            _cycleCount = targetCycle;

            return Mix();
        }

        private ulong GetCyclesForSample()
        {
            _cycleFrac += _clockRate;
            ulong cycles = _cycleFrac / _sampleRate;
            _cycleFrac -= cycles * _sampleRate;
            return cycles;
        }

        private short Mix()
        {
            float acc = 0f;

            foreach (var tone in _tones)
                acc += tone.Output ? _volumeTable[tone.Volume] : 0f;

            acc += _noise.Output ? _volumeTable[_noise.Volume] : 0f;

            acc /= 4f;
            acc *= short.MaxValue;

            if (acc > short.MaxValue) return short.MaxValue;
            if (acc < short.MinValue) return short.MinValue;
            return (short)acc;
        }


        public void Write(byte data) => _commands.Enqueue
        (
            new IncomingCommand 
            { 
                Data  = data, 
                Latch = data.IsBitSet(7), 
                Cycle = _cycleCount + 32
            }
        );

        internal void ApplyWrite(IncomingCommand command)
        {
            byte data = command.Data;

            if (command.Latch)
            {
                _latch.Channel  = (data >> 5) & 0b11;
                _latch.IsVolume = data.IsBitSet(4);
            }

            int channel = _latch.Channel;

            if (_latch.IsVolume) ApplyVolume(channel, (byte)(data & 0x0F));
            else                 ApplyChannel(command, channel, data);
        }

        private void ApplyVolume(int channel, byte volume)
        {
            if (channel < _tones.Length)
                _tones[channel].Volume = volume;
            else
                _noise.Volume = volume;
        }

        private void ApplyChannel(IncomingCommand command, int channel, byte data)
        {
            if (channel < _tones.Length)
            {
                ToneChannel tone = _tones[channel];

                tone.Period = command.Latch
                    ? (ushort)((tone.Period & 0x03F0) | (byte)(data & 0x0F))
                    : (ushort)((tone.Period & 0x000F) | ((data & 0x3F) << 4));
            }
            else
            {
                _noise.Type       = (NoiseType)data.GetBit(2);
                _noise.PeriodMode = (byte)(data & 0b11);
                _noise.LSFR       = 0x0001;
            }
        }


        private void Tick(ulong cycles)
        {
            int pres = _prescaler;

            if (cycles < (ulong)pres)
            {
                _prescaler = pres - (int)cycles;
                return;
            }

            cycles -= (ulong)pres;
            ulong ticks = 1 + (cycles >> 4);
            int rem = (int)(cycles & 0xF);

            _prescaler = 16 - rem;

            for (ulong i = 0; i < ticks; i++)
            {
                TickTones();
                TickNoise();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TickTones()
        {
            foreach (var tone in _tones)
            {
                tone.Counter--;

                if (tone.Counter == 0 && tone.Period != 0)
                {
                    tone.Counter = tone.Period;
                    tone.Output  = !tone.Output;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TickNoise()
        {
            _noise.Counter--;

            if (_noise.Counter <= 0)
            {
                ushort period = _noise.PeriodMode switch
                {
                    var i when i < 3 => (ushort)(0x10 << i),
                    3                => _tones[2].Period,
                    _                => 0
                };

                if (period > 0)
                {
                    _noise.Counter = period;
                    _noise.Edge    = !_noise.Edge;

                    if (_noise.Edge)
                    {
                        int feedback = _noise.Type switch
                        {
                            NoiseType.White    => _whiteNoiseFeedback(),
                            NoiseType.Periodic => _noise.LSFR & 1,
                            _                  => 0
                        };

                        _noise.LSFR = (ushort)((_noise.LSFR >> 1) | (feedback << _lsfrShift));
                        _noise.Output = (_noise.LSFR & 1) != 0;
                    }
                }
            }
        }


        public void Reset()
        {
            foreach (var tone in _tones)
            {
                tone.Counter = 0;
                tone.Period  = 0; 
                tone.Output  = false;
                tone.Volume  = 0x0F;
            }

            _noise.Counter    = 0; 
            _noise.LSFR       = 0x0001;
            _noise.PeriodMode = 0;
            _noise.Type       = NoiseType.Periodic;
            _noise.Output     = false;
            _noise.Edge       = false;
            _noise.Volume     = 0x0F;

            _latch.Channel  = 0;
            _latch.IsVolume = false;

            _prescaler = 16;

            _commands.Clear();
        }
    }
}