namespace SN7S.Core
{
    internal class ToneChannel(byte id)
    {
        internal readonly byte ID = id;

        internal ulong NextToggleCycle;
        internal ushort Counter;
        internal ushort Period;

        internal bool Output;
        internal byte Volume;
    }
}