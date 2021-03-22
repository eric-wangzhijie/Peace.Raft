namespace Raft.Demo.StateMachine
{
    internal class LeaderVolatileState
    {
        /// <summary>
        /// index of the next log entry to send to that server(initialized to leader last log index + 1)
        /// </summary>
        public ulong NextIndex { get; set; }

        /// <summary>
        /// index of highest log entry known to be replicated on server (initialized to 0, increases monotonically)
        /// </summary>
        public ulong MatchIndex { get; set; }
    }
}
