namespace Raft.Demo
{
    public class VoteReqeust
    {
        /// <summary>
        /// candidate’s term
        /// </summary>
        public ulong Term { get; set; }

        /// <summary>
        /// candidate requesting vote
        /// </summary>
        public string CandidateId { get; set; }

        /// <summary>
        /// index of candidate’s last log entry 
        /// </summary>
        public ulong LastLogIndex { get; set; }

        /// <summary>
        /// term of candidate’s last log entry
        /// </summary>
        public ulong LastLogTerm { get; set; }
    }
}
