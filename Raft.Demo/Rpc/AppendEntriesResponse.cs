namespace Raft.Demo
{  
    public class AppendEntriesResponse  
    {  
        /// <summary>
        /// currentTerm, for leader to update itself
        /// </summary>
        public ulong Term { get; set; }

        /// <summary>
        /// true if follower contained entry matching prevLogIndex and prevLogTerm
        /// </summary>
        public bool IsSuccess { get; set; }

    }
}
