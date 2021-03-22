using Raft.Demo.StateMachine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Raft.Demo
{
    internal class StateController
    {
        private readonly PersistentState _persistentState = new PersistentState();
        private readonly VolatileState _volatileState = new VolatileState();
        private readonly IStateMachine _stateMachine;

        private readonly ReaderWriterLockSlim _perSistentRWLS = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _volatileRWLS = new ReaderWriterLockSlim();

        public StateController(Config config, IStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
            Config = config;
        }

        public Config Config { get; }

        public PersistentState PersistentState
        {
            get
            {
                _perSistentRWLS.EnterReadLock();
                try
                {
                    return _persistentState;
                }
                finally
                {
                    _perSistentRWLS.ExitReadLock();
                }
            }
        }

        public VolatileState VolatileState
        {
            get
            {
                _volatileRWLS.EnterReadLock();
                try
                {
                    return _volatileState;
                }
                finally
                {
                    _volatileRWLS.ExitReadLock();
                }
            }
        }

        public void IncreaseCommitIndex()
        {
            _volatileRWLS.EnterWriteLock();
            try
            {
                _volatileState.CommitIndex += 1;
            }
            finally
            {
                _volatileRWLS.ExitWriteLock();
            }
        }

        /// <summary>
        /// Update current term.
        /// </summary>
        public void UpdateTerm(ulong term)
        {
            _perSistentRWLS.EnterWriteLock();
            try
            {
                DebugConsole.WriteLine($"Update current term to {term}.");
                _persistentState.CurrentTerm = term;
                _persistentState.VotedFor = null;
            }
            finally
            {
                _perSistentRWLS.ExitWriteLock();
            }
        }

        /// <summary>
        /// Update vote for.
        /// </summary>
        public void UpdateVoteFor(string votedFor)
        {
            _perSistentRWLS.EnterWriteLock();
            try
            {
                _persistentState.VotedFor = votedFor;
            }
            finally
            {
                _perSistentRWLS.ExitWriteLock();
            }
        }

        /// <summary>
        /// Update commitIndex.
        /// </summary>
        public void UpdateCommitIndex(ulong commitIndex)
        {
            _volatileRWLS.EnterWriteLock();
            try
            {
                _volatileState.CommitIndex = commitIndex;
                if (_volatileState.CommitIndex > _volatileState.LastApplied)
                {
                    //Todo  apply log[lastApplied] to state machine(§5.3)
                    _stateMachine.ApplyLog(_persistentState.Logs.Last());
                    _volatileState.LastApplied += 1;
                }
            }
            finally
            {
                _volatileRWLS.ExitWriteLock();
            }
        }
    }
}
