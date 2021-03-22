using System.Net;

namespace Raft.RPC
{
    /// <summary>
    /// 客户端存根
    /// </summary>
    public class ClientStub : StubBase
    { 
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
          
    }
}
