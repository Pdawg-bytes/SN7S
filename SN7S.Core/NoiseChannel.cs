namespace SN7S.Core
{
    internal class NoiseChannel
    {
        internal ulong NextToggleCycle;
        internal ushort Counter;
        internal ushort LSFR;

        internal byte PeriodMode;
        internal NoiseType Type;

        internal bool Output;
        internal bool Edge;
        internal byte Volume;
    }

    internal enum NoiseType : byte
    {
        Periodic,
        White
    }
}