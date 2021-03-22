using System;

namespace Raft.RPC
{
    public interface ILog
    {
        void WriteErrorLog(Exception exception);
    }
}