namespace Raft.Demo
{ 
    public class InstallSnapshotResponse  
    { 
        /// <summary>
        /// currentTerm, for leader to update itself
        /// </summary>
        public ulong Term { get; set; } 
    }
}
