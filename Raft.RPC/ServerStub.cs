using System;
using System.Collections.Generic;

namespace Raft.RPC
{
    /// <summary>
    /// 服务存根
    /// </summary>
    public class ServerStub : StubBase
    {
        private readonly Dictionary<string, ServiceBase> _serviceInstances = new Dictionary<string, ServiceBase>(); 

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="logger"></param>
        public ServerStub(string host, int port, ILog logger) : base(logger)
        {
            Host = host;
            Port = port; 
        }

        /// <summary>
        /// 监听的Ip地址
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// 监听的端口
        /// </summary>
        public int Port { get; }
         
        /// <summary>
        /// 添加服务实例
        /// </summary>
        /// <param name="serviceInterfaceType">服务接口类型</param>
        /// <param name="serviceType">服务类型</param>
        /// <returns>服务信息</returns>
        internal void AddServiceInstance(Type serviceInterfaceType, Type serviceType)
        {
            if (_serviceInstances.ContainsKey(serviceType.Name))
            {
                throw new RpcInternalException($"The service {serviceType} has registed.");
            }
            ServiceBase service = Activator.CreateInstance(serviceType) as ServiceBase; 
            _serviceInstances.Add(serviceInterfaceType.Name, service);
        }

        /// <summary>
        /// 添加服务实例
        /// </summary>
        /// <param name="serviceInterfaceType">服务接口类型</param>
        /// <param name="serviceType">服务类型</param>
        /// <param name="serviceInstance">服务对象实例</param>
        /// <returns>服务信息</returns>
        internal void AddServiceInstance(Type serviceInterfaceType, Type serviceType, ServiceBase serviceInstance)
        {
            if (_serviceInstances.ContainsKey(serviceType.Name))
            {
                throw new RpcInternalException($"The service {serviceType} has registed.");
            } 
            _serviceInstances.Add(serviceInterfaceType.Name, serviceInstance);
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <param name="serviceInterfaceTypeName">服务接口类型</param>
        /// <returns>服务信息</returns>
        internal object GetServiceInstance(string serviceInterfaceTypeName)
        {
            if (_serviceInstances.ContainsKey(serviceInterfaceTypeName))
            {
                return _serviceInstances[serviceInterfaceTypeName];
            }
            return null;
        }
    }
}
