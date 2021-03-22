using Raft.RPC;

namespace Raft.Demo
{
    internal class Host : ServiceBase, IHost
    {
        private readonly StateController _stateController;
        private readonly Node _node;

        internal Host(StateController stateController, Node node)
        {
            _node = node;
            _stateController = stateController;
        }

        public ClientResponse ClientInvoke(ClientReqeust request)
        {
            lock (this)
            {
                if (_node.CurrentRole.Type == RoleType.Follower)
                {
                    Follower follower = (Follower)_node.CurrentRole; 
                    //todo rediret request to leader node
                    IHost _host = _node.Peers[0].RemoteClient;
                    return _host.ClientInvoke(request);
                }
                return (_node.CurrentRole as Leader).Command(request);
            }
        }

        public AppendEntriesResponse AppendEntriesInvoke(AppendEntriesRequest reqeust)
        {
            lock (this)
            {
                DebugConsole.WriteLine($"Append entries from leader(Id:{reqeust.LeaderId} Term:{reqeust.Term}...");
                _node.EnsureExistGreaterTermAndChangeRole(reqeust.Term);

                if (reqeust.Term < _stateController.PersistentState.CurrentTerm)
                {
                    return new AppendEntriesResponse()
                    {
                        Term = _stateController.PersistentState.CurrentTerm,
                        IsSuccess = false
                    };
                }

                if (_node.CurrentRole.Type == RoleType.Candidate && reqeust.Term == _stateController.PersistentState.CurrentTerm)
                {
                    _node.ChangeRole(RoleType.Follower);
                }
                return ((Follower)_node.CurrentRole).AppendEntries(reqeust);
            }
        }

        public VoteResponse VoteInvoke(VoteReqeust reqeust)
        {
            lock (this)
            {
                _node.EnsureExistGreaterTermAndChangeRole(reqeust.Term);
                if (_node.CurrentRole.Type != RoleType.Follower)
                {
                    return new VoteResponse()
                    {
                        Term = _stateController.PersistentState.CurrentTerm,
                        VoteGranted = false
                    };
                }
                return ((Follower)_node.CurrentRole).GetVote(reqeust);
            }
        }

        public InstallSnapshotResponse InstalledSnapshotInvoke(InstallSnapshotReqeust reqeust)
        {
            lock (this)
            {
                _node.EnsureExistGreaterTermAndChangeRole(reqeust.Term);
                return ((Follower)_node.CurrentRole).InstalledSnapshot(reqeust);
            }
        }
    }
}
