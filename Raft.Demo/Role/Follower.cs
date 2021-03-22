using Raft.Demo.StateMachine;
using System;
using System.Linq;

namespace Raft.Demo
{
    internal class Follower : Role
    {
        private bool _isFromLegalLeaderRequest = false;
        private readonly StateController _stateController;
        private readonly Node _node;

        public override RoleType Type => RoleType.Follower;

        public override string Id => _stateController.Config.NodeId;

        public string LeaderId { get; private set; }

        internal Follower(StateController stateController, Node node)  
        {
            _node = node;
            _stateController = stateController;
            Alarm.StartBeforeTimewait(Reclaim);
        }

        public VoteResponse GetVote(VoteReqeust request)
        {
            VoteResponse response = new VoteResponse();
            PersistentState state = _stateController.PersistentState;
            if (request.Term < state.CurrentTerm)
            {
                response.VoteGranted = false;
            }
            else if ((string.IsNullOrEmpty(state.VotedFor) || state.VotedFor == request.CandidateId) && request.LastLogTerm >= state.LastLogTerm && request.LastLogIndex >= state.LastLogIndex)
            {
                _stateController.UpdateVoteFor(request.CandidateId);
                response.VoteGranted = true;
                DebugConsole.WriteLine($"Voting for candidate {request.CandidateId} for term {request.Term}...");
            }
            response.Term = state.CurrentTerm;
            return response;
        }

        public AppendEntriesResponse AppendEntries(AppendEntriesRequest request)
        {
            if (string.IsNullOrEmpty(LeaderId))
            {
                LeaderId = request.LeaderId;
            }
            if (request.Term == _stateController.PersistentState.CurrentTerm)
            {
                lock (this)
                {
                    _isFromLegalLeaderRequest = true;
                }
            }
            AppendEntriesResponse response = new AppendEntriesResponse();
            if (_stateController.PersistentState.Logs.Any())
            {
                LogEntry logEntry = _stateController.PersistentState.Logs.SingleOrDefault(w => w.Index == request.PrevLogIndex && w.Term == request.PrevLogTerm);
                if (logEntry == null)
                {
                    response.IsSuccess = false;
                    response.Term = _stateController.PersistentState.CurrentTerm;
                    return response;
                }

                LogEntry diffTermLogEntry = _stateController.PersistentState.Logs.SingleOrDefault(w => request.Entries.Exists(e => e.Index == w.Index && w.Term != e.Term));
                if (diffTermLogEntry != null)
                {
                    _stateController.PersistentState.Logs.RemoveRange((int)diffTermLogEntry.Index, _stateController.PersistentState.Logs.Count - (int)diffTermLogEntry.Index);
                }
                _stateController.PersistentState.Logs.AddRange(request.Entries);
            }

            if (request.LeaderCommit > _stateController.VolatileState.CommitIndex)
            {
                _stateController.UpdateTerm(request.Term);
                _stateController.UpdateCommitIndex(Math.Min(request.LeaderCommit, request.PrevLogIndex));
            }
            response.IsSuccess = true;
            response.Term = _stateController.PersistentState.CurrentTerm;
            return response;
        }

        public InstallSnapshotResponse InstalledSnapshot(InstallSnapshotReqeust request)
        {
            InstallSnapshotResponse response = new InstallSnapshotResponse();
            if (request.Term < _stateController.PersistentState.CurrentTerm)
            {
                response.Term = _stateController.PersistentState.CurrentTerm;
                return response;
            }
            if (request.Offset == 0)
            {
                // todo create a new snapshot
            }
            //在指定偏移量写入数据
            //如果 done 是 false，则继续等待更多的数据
            //保存快照文件，丢弃具有较小索引的任何现有或部分快照
            //如果现存的日志条目与快照中最后包含的日志条目具有相同的索引值和任期号，则保留其后的日志条目并进行回复
            //丢弃整个日志
            //使用快照重置状态机（并加载快照的集群配置）
            return response;
        }

        private void Reclaim()
        {
            try
            {
                if (!_isFromLegalLeaderRequest)
                {
                    DebugConsole.WriteLine("Missing leader..." + Environment.NewLine);
                    _node.ChangeRole(RoleType.Candidate);
                }
                else
                {
                    lock (this)
                    {
                        _isFromLegalLeaderRequest = false;
                    }
                }
            }
            catch (Exception e)
            {
                DebugConsole.WriteLine(e.Message + Environment.NewLine);
            }
        }
    }
}
