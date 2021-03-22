using Raft.Demo.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Raft.Demo
{
    /// <summary>
    /// leader 
    /// </summary>
    internal class Leader : Role
    {
        private readonly StateController _stateController;
        private readonly Dictionary<string, LeaderVolatileState> _leaderVolatileStates = new Dictionary<string, LeaderVolatileState>();
        private readonly List<LogEntry> _uncommittiedLogs = new List<LogEntry>();
        private readonly object _appliedLogLockObj = new object();
        private readonly Node _node;

        public override RoleType Type => RoleType.Leader;

        public override string Id => _stateController.Config.NodeId;

        internal Leader(StateController stateController, Node node)
        {
            _node = node;
            _stateController = stateController;
            InitialLeaderVolatileState();
            Alarm.Start(Heartbeat, 500);
        }

        public ClientResponse Command(ClientReqeust reqeust)
        {
            ClientResponse response = new ClientResponse();
            LogEntry logEntry = new LogEntry
            {
                Command = reqeust.Command,
                Index = _stateController.PersistentState.LastLogIndex + 1,
                Term = _stateController.PersistentState.CurrentTerm
            };
            _uncommittiedLogs.Add(logEntry);
            return response;
        }

        private void InitialLeaderVolatileState()
        {
            foreach (Peer peer in _node.Peers)
            {
                LeaderVolatileState state = new LeaderVolatileState
                {
                    NextIndex = _stateController.PersistentState.LastLogIndex,
                    MatchIndex = 0
                };
                _leaderVolatileStates.Add(peer.Address, state);
            }
        }

        private void CommitLogs()
        {
            if (_uncommittiedLogs.Any())
            {
                _stateController.PersistentState.Logs.AddRange(_uncommittiedLogs);
                _stateController.IncreaseCommitIndex();
            }
            // todo  apply log to statemachine
        }

        private AppendEntriesResponse AppendEntries(string address, IHost host, ulong nextIndex)
        {
            AppendEntriesRequest request = new AppendEntriesRequest
            {
                LeaderId = Id,
                Term = _stateController.PersistentState.CurrentTerm,
                PrevLogIndex = _stateController.PersistentState.LastLogIndex,
                PrevLogTerm = _stateController.PersistentState.LastLogTerm,
                LeaderCommit = _stateController.VolatileState.CommitIndex
            };
            if (_stateController.PersistentState.LastLogIndex >= nextIndex)
            {
                List<LogEntry> logEntries = _uncommittiedLogs.FindAll(w => w.Index >= nextIndex);
                if (logEntries != null && logEntries.Any())
                {
                    request.Entries = logEntries;
                }
            }

            AppendEntriesResponse response = host.AppendEntriesInvoke(request);
            if (!response.IsSuccess && _leaderVolatileStates[address].NextIndex != 0)
            {
                _leaderVolatileStates[address].NextIndex -= 1;
                response = AppendEntries(address, host, _leaderVolatileStates[address].NextIndex);
            }
            return response;
        }

        private void Heartbeat()
        {
            bool committedLog = false;
            int replicatedCount = 1;
            foreach (Peer peer in _node.Peers)
            {
                Task.Run(() =>
                  {
                      try
                      {
                          DebugConsole.WriteLine($"Sync to follower({peer.Address})... term {_stateController.PersistentState.CurrentTerm}...");
                          AppendEntriesResponse response = AppendEntries(peer.Address, peer.RemoteClient, _leaderVolatileStates[peer.Address].NextIndex);
                          if (_node.EnsureExistGreaterTermAndChangeRole(response.Term))
                          {
                              return;
                          }

                          if (response.IsSuccess)
                          {
                              lock (_appliedLogLockObj)
                              {
                                  _leaderVolatileStates[peer.Address].NextIndex = _stateController.PersistentState.LastLogIndex + 1;
                                  _leaderVolatileStates[peer.Address].MatchIndex += 1;

                                  replicatedCount += 1;
                                  int majorityCount = _node.Peers.Count;
                                  if (!committedLog && replicatedCount > majorityCount / 2 + 1)
                                  {
                                      committedLog = true;
                                      CommitLogs();
                                  }
                              }
                          }
                      }
                      catch (Exception ex)
                      {
                          DebugConsole.WriteLine(ex.Message);
                          //todo
                      }
                  });
            }
        }
    }
}
