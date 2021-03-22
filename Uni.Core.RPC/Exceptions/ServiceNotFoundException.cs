using System;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 未找到可用的服务异常
    /// </summary>
    class ServiceNotFoundException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="content">异常信息</param>
        public ServiceNotFoundException(string content) : base(content)
        {
        }
    }
}
