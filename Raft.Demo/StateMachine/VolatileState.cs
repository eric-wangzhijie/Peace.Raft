namespace Raft.Demo
{
    internal class VolatileState
    {
        /// <summary>
        /// index of highest log entry known to be committed(initialized to 0, increases monotonically)
        /// </summary>
        public ulong CommitIndex { get; set; }

        /// <summary>
        /// index of highest log entry applied to state machine(initialized to 0, increases monotonically)
        /// </summary>
        public ulong LastApplied { get; set; }
    }
}
