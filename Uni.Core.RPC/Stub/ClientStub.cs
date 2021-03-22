using System.Net;
using Uni.Common;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 客户端存根
    /// </summary>
    public class ClientStub : StubBase
    {
        private readonly ClientConnectionPool _connectionPool; 

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host">远程服务地址ip</param>
        /// <param name="port">远程服务地址端口</param>
        /// <param name="clusterToken">token</param>
        /// <param name="logger">日志</param>
        public ClientStub(string host, int port, string clusterToken, ILog logger) : base(logger)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(host), port); 
            ClusterToken = clusterToken;
            EnableServiceDiscovery = false;
        }

        /// <summary>
        /// 构造函数
        /// </summary>  
        /// <param name="clusterToken">token</param>
        /// <param name="logger">日志</param>
        /// <param name="redisConnection">注册中心连接地址</param>
        public ClientStub(string clusterToken, ILog logger, string redisConnection, ITraceChain traceChain) : base(logger)
        {
            ClusterToken = clusterToken;
            EnableServiceDiscovery = true;
            TraceChain = traceChain;
            _connectionPool = new ClientConnectionPool(new ServiceDiscovery(redisConnection));
            this.Functions.Add(uint.MinValue, _connectionPool.RefreshAll);
        }

        /// <summary>
        /// 集群token
        /// </summary>
        public string ClusterToken { get; }

        /// <summary>
        /// 远程服务端地址
        /// </summary>
        public IPEndPoint EndPoint { get; } 

        /// <summary>
        /// 启用注册中心服务发现
        /// </summary>
        public bool EnableServiceDiscovery { get; }

        /// <summary>
        /// 链路管理
        /// </summary>
        public ITraceChain TraceChain { get; }

        /// <summary> 
        /// 获取可用的服务连接数量
        /// </summary>
        /// <param name="serviceName">服务名称</param> 
        /// <returns>一个可用的服务地址信息</returns>
        public int GetConnectionCount(string serviceName)
        {
            return _connectionPool.GetCount(serviceName);
        }

        /// <summary>
        /// 移除一个服务的连接
        /// </summary>
        /// <param name="serviceName">服务地址</param>
        public void Remove(string serviceName)
        {
            _connectionPool.Remove(serviceName);
        }

        /// <summary> 
        /// 获取一个的连接
        /// </summary>
        /// <param name="serviceName">服务名称</param> 
        /// <returns>一个可用的服务地址信息</returns>
        public ClientConnection EnsureConnection(string serviceName)
        {
            return _connectionPool.Ensure(serviceName);
        }
    }
}
