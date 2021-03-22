using System;
using Uni.Common;

namespace Uni.Core.RPC.DotNetty
{
    /// <summary>
    /// DotNetty通讯管理
    /// </summary>
    public class TCPChannel : AbstractChannel
    {
        /// <summary>
        /// dotNetty 客户端通讯管理
        /// </summary> 
        /// <param name="host">要连接的远程服务ip</param>
        /// <param name="port">要连接的远程服务端口</param>
        /// <param name="clusterToken">集群授权码</param>
        /// <param name="logger">日志</param>
        public TCPChannel(string host, int port, string clusterToken, ILog logger) : base(host, port, clusterToken, logger)
        {
        }

        /// <summary>
        /// dotNetty 客户端通讯管理,当前模式下使用注册中心发现服务
        /// </summary>  
        /// <param name="clusterToken">集群授权码</param>
        /// <param name="logger">日志</param>
        /// <param name="redisConnection">注册中心redis</param>
        /// <param name="traceChain">链路管理</param>
        public TCPChannel(string clusterToken, ILog logger, string redisConnection, ITraceChain traceChain) : base(clusterToken, logger, redisConnection, traceChain)
        {
        }

        /// <summary>
        /// 获取客户端访问类
        /// </summary>
        /// <param name="serviceType">服务元数据信息</param>
        /// <param name="clientStub">存根</param>
        /// <returns>客户端访问对象</returns>
        protected override IClient InitializeClient(Type serviceType, ClientStub clientStub)
        {
            return new TCPClient(clientStub, serviceType);
        }
    }
}
