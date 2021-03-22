namespace Raft.Demo
{ 
    public class VoteResponse 
    { 
        /// <summary>
        /// currentTerm, for candidate to update itself
        /// </summary>
        public ulong Term { get; set; }

        /// <summary>
        /// true means candidate received vote
        /// </summary>
        public bool VoteGranted { get; set; }
    }
}
