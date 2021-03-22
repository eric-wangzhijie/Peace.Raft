using Raft.Demo.StateMachine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Raft.Demo
{
    internal class Candidate : Role
    {
        private readonly object _transferedLeaderLockObj = new object();
        private readonly StateController _stateController;
        private readonly Node _node;

        public override RoleType Type => RoleType.Candidate;

        public override string Id => _stateController.Config.NodeId;

        internal Candidate(StateController stateController, Node node)  
        {
            _node = node;
            _stateController = stateController;
            Alarm.StartAfterTimewait(RequestVote);
        }

        private VoteResponse RequestVoteCore(IHost host)
        {
            PersistentState state = _stateController.PersistentState;
            VoteReqeust request = new VoteReqeust
            {
                CandidateId = Id,
                Term = state.CurrentTerm,
                LastLogIndex = state.LastLogIndex,
                LastLogTerm = state.LastLogTerm
            };
            return host.VoteInvoke(request);
        }

        private void RequestVote()
        {
            _stateController.UpdateTerm(_stateController.PersistentState.CurrentTerm + 1);
            DebugConsole.WriteLine($"Starting election ... term :{_stateController.PersistentState.CurrentTerm}" + Environment.NewLine);
            //Vote for current object
            int votes = 1;
            bool transferedLeader = false;

            List<Task> taskList = new List<Task>();
            int majorityCount = _node.Peers.Count;
            foreach (Peer peer in _node.Peers)
            {
                Task task = Task.Run(() =>
                {
                    try
                    {
                        VoteResponse response = RequestVoteCore(peer.RemoteClient);
                        if (_node.EnsureExistGreaterTermAndChangeRole(response.Term))
                        {
                            return;
                        }

                        if (response.VoteGranted)
                        {
                            lock (_transferedLeaderLockObj)
                            {
                                votes += 1;
                                if (!transferedLeader && votes > majorityCount / 2 + 1)
                                {
                                    transferedLeader = true;
                                    _node.ChangeRole(RoleType.Leader);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.WriteLine($"{ex.Message}");
                    }
                });
                taskList.Add(task);
            }
            Task.WaitAll(taskList.ToArray());
        }
    }
}
