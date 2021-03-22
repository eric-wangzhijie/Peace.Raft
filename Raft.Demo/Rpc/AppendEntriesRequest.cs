using System.Collections.Generic;

namespace Raft.Demo
{
    /// <summary>
    /// append entries request form leader server
    /// </summary>
     
    public class AppendEntriesRequest 
    { 
        /// <summary>
        /// leader’s term
        /// </summary>
        public ulong Term { get; set; }

        /// <summary>
        /// so follower can redirect clients
        /// </summary>
        public string LeaderId { get; set; }

        /// <summary>
        /// index of log entry immediately preceding new ones
        /// </summary>
        public ulong PrevLogIndex { get; set; }

        /// <summary>
        /// term of prevLogIndex entry
        /// </summary>
        public ulong PrevLogTerm { get; set; }

        /// <summary>
        /// log entries to store (empty for heartbeat;may send more than one for efficiency)
        /// </summary>
        public List<LogEntry> Entries { get; set; }

        /// <summary>
        /// leader’s commitIndex
        /// </summary>
        public ulong LeaderCommit { get; set; }
    }
}
