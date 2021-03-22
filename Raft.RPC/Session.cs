namespace Raft.RPC
{
    public class Session
    {
        public const string NETTY_CHANNEL_KEY = "NETTY_CHANNEL_KEY";

        public string SessionId { get; set; }
    }
}
