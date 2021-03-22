namespace Raft.Demo
{
    public class Peer
    {
        public Peer(string address, IHost remoteClient)
        {
            Address = address;
            RemoteClient = remoteClient;
        }
         
        public IHost RemoteClient { get; }

        public string Address { get; }

        public string VoteGranted { get; set; }

        public string RPCDue { get; set; }

        public string HeartbeatDue { get; set; }
    }
}