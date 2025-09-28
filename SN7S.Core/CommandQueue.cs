namespace SN7S.Core
{
    internal sealed class CommandQueue
    {
        private readonly Queue<IncomingCommand> _queue = new();

        internal int Count => _queue.Count;
        internal IncomingCommand Peek() => _queue.Peek();
        internal IncomingCommand Dequeue() => _queue.Dequeue();
        internal void Enqueue(IncomingCommand cmd) => _queue.Enqueue(cmd);
        internal void Clear() => _queue.Clear();
    }

    internal struct IncomingCommand
    {
        internal byte Data;
        internal bool Latch;
        internal ulong Cycle;
    }

    internal struct LatchCommand
    {
        internal int Channel;
        internal bool IsVolume;
    }
}