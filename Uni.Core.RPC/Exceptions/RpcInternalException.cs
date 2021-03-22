using System;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 远程调用异常
    /// </summary>
    class RpcInternalException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="content"></param>
        public RpcInternalException(string content) : base(content)
        {
        }
    }
}
