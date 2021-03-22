using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 服务发现
    /// </summary>
    public class ServiceDiscovery
    {
        private readonly ServiceDirectoryProxy _directory;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="redisConnection">注册中心redis连接地址</param>
        public ServiceDiscovery(string redisConnection)
        {
            _directory = new ServiceDirectoryProxy(redisConnection);
        }

        /// <summary>
        /// 根据条件查询服务的实例
        /// </summary>  
        /// <param name="serviceName">服务的名称</param>
        /// <returns>服务实例的实例列表，格式是(小写的服务实例Id，服务实例)</returns>
        public async Task<List<ServiceDescription>> GetService(string serviceName)
        {
            return await this._directory.LookFor(serviceName);
        }
    }
}
