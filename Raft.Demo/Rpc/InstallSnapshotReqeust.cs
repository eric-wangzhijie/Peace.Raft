namespace Raft.Demo
{ 
    public class InstallSnapshotReqeust  
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
        /// the snapshot replaces all entries up through and including this index
        /// </summary>
        public ulong LastIncludedIndex { get; set; }

        /// <summary>
        /// term of lastIncludedIndex
        /// </summary>
        public ulong LastIncludedTerm { get; set; }

        /// <summary>
        /// byte offset where chunk is positioned in the snapshot file
        /// </summary>
        public ulong Offset { get; set; }

        /// <summary>
        /// raw bytes of the snapshot chunk, starting at offset
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// true if this is the last chunk
        /// </summary>
        public ulong Done { get; set; }

    }
}
