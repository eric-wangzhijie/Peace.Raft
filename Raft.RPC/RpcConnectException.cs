using System;

namespace Raft.RPC
{
    /// <summary>
    /// 远程连接失败异常
    /// </summary>
    class RpcConnectException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message"></param>
        public RpcConnectException(string message) : base(message)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public RpcConnectException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
