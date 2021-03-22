namespace Raft.Demo
{
    public class LogEntry
    {
        public ulong Term { get; set; }

        public ulong Index { get; set; }

        public ICommand Command { get; set; }
    }
}
