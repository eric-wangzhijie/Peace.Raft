namespace Raft.Demo
{
    public class Config
    {
        public string NodeId { get; set; }

        public string NodeName { get; set; }

        public string LocalAddress { get; set; }

        public string[] JoinAddresses { get; set; }

        public string ClusterToken { get; set; }

        public int RetryCount { get; set; }
    }
}
