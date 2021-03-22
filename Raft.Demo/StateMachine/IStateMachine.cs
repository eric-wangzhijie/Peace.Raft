using System.Threading.Tasks;

namespace Raft.Demo.StateMachine
{
    interface IStateMachine
    { 
        Task ApplyLog(LogEntry log);
    }
}
