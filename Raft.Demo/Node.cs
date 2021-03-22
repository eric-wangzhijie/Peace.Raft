using Raft.Demo.StateMachine;
using Raft.RPC.Grpc;
using System.Collections.Generic;

namespace Raft.Demo
{
    public class Node
    {
        private readonly object _ensureTermLockObj = new object();
        private readonly StateController _stateController;
        private readonly Config _config;
        private readonly IHost _host;

        public Node(Config config)
        {
            _config = config;
            _stateController = new StateController(config, new FileStateMachine(config.NodeId));
            Peers = new List<Peer>();
            foreach (string address in config.JoinAddresses)
            {
                if (address == config.LocalAddress)
                {
                    continue;
                }
                string[] segments = address.Split(':');
                GrpcChannel channel = new GrpcChannel(segments[0], int.Parse(segments[1]), config.ClusterToken, new DebugConsole());
                Peers.Add(new Peer(address, channel.GetClient<IHost>()));
            }

            _host = new Host(_stateController, this);
            CurrentRole = new Follower(_stateController, this);
        }

        internal IList<Peer> Peers { get; }

        internal Role CurrentRole { get; private set; }

        /// <summary>
        /// 启动服务
        /// </summary>
        public void Start()
        {
            GrpcServer server = new GrpcServer(_config.LocalAddress.Split(':')[0], int.Parse(_config.LocalAddress.Split(':')[1]), _config.ClusterToken, new DebugConsole());
            server.Register(typeof(IHost), typeof(Host), _host);
            server.Start().Wait();

            DebugConsole.WriteLine("Raft is Running...");
            DebugConsole.WriteLine($"NodeId:{_config.NodeId} name:{_config.NodeName} address:{_config.LocalAddress}...");
        }

        internal void ChangeRole(RoleType role)
        {
            if (CurrentRole.Type != role)
            {
                CurrentRole.Alarm.Stop();
                DebugConsole.WriteLine($"Changing role to {role} from {CurrentRole.Type}...");
                switch (role)
                {
                    case RoleType.Leader:
                        CurrentRole = new Leader(_stateController, this);
                        break;
                    case RoleType.Follower:
                        CurrentRole = new Follower(_stateController, this);
                        break;
                    case RoleType.Candidate:
                        CurrentRole = new Candidate(_stateController, this);
                        break;
                }
            }
            DebugConsole.WriteLine($"Current role is {CurrentRole.Type}...");
        }

        internal bool EnsureExistGreaterTermAndChangeRole(ulong term)
        {
            lock (_ensureTermLockObj)
            {
                if (term > _stateController.PersistentState.CurrentTerm)
                {
                    DebugConsole.WriteLine($"Existed a term greater than current term.");
                    _stateController.UpdateTerm(term);
                    ChangeRole(RoleType.Follower);
                    return true;
                }
                return false;
            }
        }
    }
}
