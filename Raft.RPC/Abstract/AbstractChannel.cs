using ImpromptuInterface;
using System;
using System.Collections.Concurrent;

namespace Raft.RPC
{
    /// <summary>
    /// 客户端通讯管理抽象类
    /// </summary>
    public abstract class AbstractChannel
    {
        private readonly ClientStub _clientStub;

        /// <summary>
        /// [serviceName/client object]
        /// </summary>
        private ConcurrentDictionary<string, object> _services = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// 构造函数
        /// </summary> 
        /// <param name="clusterToken">集群授权码</param>
        internal AbstractChannel(string host, int port, string clusterToken, ILog logger)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (port < 0 || port > ushort.MaxValue)
            {
                throw new ArgumentException(nameof(port));
            }
            if (string.IsNullOrEmpty(clusterToken))
            {
                throw new ArgumentNullException(nameof(clusterToken));
            }
            _clientStub = new ClientStub(host, port, clusterToken, logger);
        }
         
        /// <summary>
        /// 获取客户端访问类
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="clientStub">存根</param>
        /// <returns>客户端访问对象</returns>
        protected abstract IClient InitializeClient(Type serviceType, ClientStub clientStub);

        /// <summary>
        /// 获取对应服务的客户端
        /// </summary>
        /// <typeparam name="T">服务泛型</typeparam>
        /// <returns>服务对应实例</returns>
        public T GetClient<T>() where T : class
        {
            var type = typeof(T);
            var serviceName = type.Name;
            if (_services.ContainsKey(serviceName))
            {
                return _services[serviceName] as T;
            }
            var client = InitializeClient(type, _clientStub);
            //创建代理
            T instance = client.ActLike<T>();
            _services.TryAdd(serviceName, instance);
            return instance;
        }
    }
}
