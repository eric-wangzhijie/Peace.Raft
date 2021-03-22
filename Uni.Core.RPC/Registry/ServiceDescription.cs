using System;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 要注册的服务描述
    /// </summary> 
    public class ServiceDescription
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ServiceDescription()
        { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">服务实例的Id</param> 
        /// <param name="serviceName">服务实例的名称</param> 
        public ServiceDescription(string id, string serviceName, string host, int port)
        {
            ServiceName = serviceName;
            Id = id;
            Host = host;
            Port = port;
        }

        /// <summary>
        /// 服务实例的Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 服务实例的主机ip
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 服务实例的端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 服务的名称
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 生效时间 对于无状态服务来说，注册的时候即生效时间,对于Shard服务来说，ShardKey生效的时候才是生效时间。
        /// </summary>
        public DateTime EffectiveTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后一次心跳线的时间
        /// </summary>
        public DateTime LastEchoTime { get; set; }

        /// <summary>
        /// 重载转化为字符串
        /// </summary>
        /// <returns>字符串</returns>
        public override string ToString()
        {
            return $"ServiceId={Id}, serviceName={ServiceName}, address={Host}:{Port}, EffectiveTime={EffectiveTime}, LastEchoTime={LastEchoTime}";
        }
    }
}