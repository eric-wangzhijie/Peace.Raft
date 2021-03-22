using System.Collections.Generic;
using System.Linq;

namespace Raft.Demo.StateMachine
{
    internal class PersistentState
    {
        /// <summary>
        /// latest term server has seen (initialized to 0 on first boot, increases monotonically)
        /// </summary>
        public ulong CurrentTerm { get; set; }

        /// <summary>
        /// candidateId that received vote in current term(or null if none)
        /// </summary>
        public string VotedFor { get; set; }

        /// <summary>
        /// log entries; each entry contains command for state machine, and term when entry was received by leader(first index is 1)
        /// </summary>
        public List<LogEntry> Logs { get; } = new List<LogEntry>();

        public ulong LastLogTerm
        {
            get
            {
                return Logs.LastOrDefault()?.Term ?? 0;
            }
        }

        public ulong LastLogIndex
        {
            get
            {
                return Logs.LastOrDefault()?.Index ?? 0;
            }
        }
    }
}
