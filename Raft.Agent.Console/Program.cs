using Raft.Demo;

namespace Raft.Agent.Console
{
    class Program
    {
        static void Main(string[] args)
        { 
            Config config = new Config();
            config.NodeId = RaftSettings.Items.NodeId;
            config.NodeName = RaftSettings.Items.NodeName;
            config.LocalAddress = RaftSettings.Items.LocalAddress;
            config.ClusterToken = RaftSettings.Items.ClusterToken;
            config.JoinAddresses = RaftSettings.Items.JoinAddresses;
            Node node = new Node(config);
            node.Start();
            System.Console.Read();
        }
    }
}
