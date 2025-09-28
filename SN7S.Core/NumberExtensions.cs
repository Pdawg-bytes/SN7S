using System.Runtime.CompilerServices;

namespace SN7S.Core
{
    internal static class NumberExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte GetBit(this byte value, int bit) => (byte)((value >> bit) & 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsBitSet(this byte value, int bit) => ((value >> bit) & 1) != 0;
    }
}