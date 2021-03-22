using System;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 集群令牌未授权
    /// </summary>
    class IllegalClusterTokenException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="content">异常信息</param>
        public IllegalClusterTokenException(string content) : base(content)
        {
        }
    }
}
