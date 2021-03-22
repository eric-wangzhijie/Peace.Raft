using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 服务连接池
    /// </summary>
    public class ClientConnectionPool
    {
        /// <summary>
        /// 控制往注册中心获取数据的频率，防止造成注册中心的压力过大
        /// </summary>
        private const int RemoveFaildServiceIntervalSeconds = 5 * 60;

        /// <summary>
        /// 每间隔3分钟刷新所有当前已经发现的服务列表
        /// </summary>
        private const int RefreshAllIntervalSeconds = 3 * 60;

        /// <summary>
        /// [services / address list]
        /// </summary>
        private readonly Dictionary<string, Queue<ClientConnection>> _peers = new Dictionary<string, Queue<ClientConnection>>();

        /// <summary>
        /// [services / refresh time]
        /// </summary>
        private readonly Dictionary<string, DateTime> _serviceRefreshTime = new Dictionary<string, DateTime>(); 
        private readonly object _lockObject = new object(); 
        private readonly ServiceDiscovery _serviceDiscovery;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ClientConnectionPool(ServiceDiscovery serviceDiscovery)
        {
            _serviceDiscovery = serviceDiscovery;
        }

        /// <summary> 
        /// 获取一个的连接
        /// </summary>
        /// <param name="serviceName">服务名称</param> 
        /// <returns>一个可用的服务地址</returns>
        public ClientConnection Ensure(string serviceName)
        {
            lock (_lockObject)
            {
                if (!_peers.ContainsKey(serviceName))
                {
                    Refresh(new[] { serviceName });
                }

                if (_peers.ContainsKey(serviceName))
                {
                    Queue<ClientConnection> q = _peers[serviceName];
                    ClientConnection conn = q.Dequeue();

                    //如果服务请求失败长达3分钟仍未恢复则拿掉该服务地址
                    if (conn.LastFailedTime != DateTime.MinValue && DateTime.Now.Subtract(conn.LastFailedTime).TotalSeconds > RefreshAllIntervalSeconds)
                    {
                        return null;
                    }

                    q.Enqueue(conn);
                    return conn;
                }
                return null;
            }
        }

        /// <summary> 
        /// 获取可用的服务连接数量
        /// </summary>
        /// <param name="serviceName">服务名称</param> 
        /// <returns>一个可用的服务地址</returns>
        public int GetCount(string serviceName)
        {
            lock (_lockObject)
            {
                if (!_peers.ContainsKey(serviceName))
                {
                    Refresh(new[] { serviceName });
                }

                int total = 0;
                if (_peers.ContainsKey(serviceName))
                {
                    Queue<ClientConnection> q = _peers[serviceName];
                    total = q.Count;
                }
                return total;
            }
        }

        /// <summary>
        /// 移除一个服务的连接
        /// </summary>
        /// <param name="serviceName">服务地址</param>
        public void Remove(string serviceName)
        {
            lock (_lockObject)
            {
                if (_peers.ContainsKey(serviceName))
                {
                    _peers.Remove(serviceName);
                    _serviceRefreshTime.Remove(serviceName);
                }
            }
        }

        private void Refresh(string[] serviceNames)
        {
            foreach (string serviceName in serviceNames)
            {
                //如果服务请求失败长达5分钟仍未恢复则移除该服务并跳过该服务检查，防止雪崩
                if (_serviceRefreshTime.ContainsKey(serviceName) && DateTime.Now.Subtract(_serviceRefreshTime[serviceName]).TotalSeconds > RemoveFaildServiceIntervalSeconds)
                {
                    Remove(serviceName);
                    continue;
                }

                List<ServiceDescription> list = _serviceDiscovery.GetService(serviceName).Result;
                if (list != null && list.Any())
                {
                    Queue<ClientConnection> clientConnections = new Queue<ClientConnection>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        clientConnections.Enqueue(new ClientConnection
                        {
                            Available = true,
                            Host = list[i].Host,
                            Port = list[i].Port,
                            ServiceId = list[i].Id
                        });
                    }
                    if (_peers.ContainsKey(serviceName))
                    {
                        _peers[serviceName] = clientConnections;
                        _serviceRefreshTime[serviceName] = DateTime.Now;
                    }
                    else
                    {
                        _peers.Add(serviceName, clientConnections);
                        _serviceRefreshTime.Add(serviceName, DateTime.Now);
                    }
                }
            }
        }

        private DateTime _now = DateTime.Now;

        /// <summary>
        /// 更新所有可用服务地址
        /// </summary>
        /// <returns></returns>
        public Task RefreshAll()
        {
            if (DateTime.Now.Subtract(_now).TotalSeconds < RefreshAllIntervalSeconds)
            {
                return Task.CompletedTask;
            }
            _now = DateTime.Now;

            lock (_lockObject)
            {
                var serviceNames = _peers.Select(s => s.Key);
                if (serviceNames != null && serviceNames.Any())
                {
                    Refresh(serviceNames.ToArray());
                }
                return Task.CompletedTask;
            }
        }
    }
}
