﻿using Grpc.Core;
using MessagePack;
using MessagePack.Resolvers;
using System.Threading.Tasks;
using Uni.Common;

namespace Uni.Core.RPC.Grpc
{
    /// <summary>
    /// dotNetty服务端
    /// </summary>
    public class GrpcServer : AbstractServer<byte[]>
    {
        private readonly Server _grpcServer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// /// <param name="id">服务实例的id，目前每个服务仅允许有一个服务实例</param>
        /// <param name="host">主机ip地址</param>
        /// <param name="port">主机端口</param>
        /// <param name="clusterToken">集群授权码</param>
        /// <param name="logger">日志</param>
        /// <param name="redisConnection">注册中心redis连接地址</param>
        public GrpcServer(string id, string host, int port, string clusterToken, ILog logger, string redisConnection)
            : base(id, host, port, clusterToken, logger, redisConnection)
        { 
            _grpcServer = new Server()
            {
                Services = { GlueServiceBinder.BindService(new GlueService(this)) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        protected override Task StartCore()
        {
            _grpcServer.Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 序列化数据
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        protected override byte[] SerializeMessage(MessageResponse response)
        {
            if (response.Data != null)
            {
                response.Data = MessagePackSerializer.Serialize(response.Data, ContractlessStandardResolver.Options);
            }
            byte[] data = MessagePackSerializer.Serialize(response, ContractlessStandardResolver.Options);
            return data;
        }

        /// <summary>
        /// 反序列化数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        protected override MessageRequest DeserializeMessage(byte[] buffer)
        {
            var request = MessagePackSerializer.Deserialize<MessageRequest>(buffer, ContractlessStandardResolver.Options);
            MethodReflectionInfo methodInfo = EnsureMethodInfo(request);
            for (int j = 0; j < methodInfo.Parameters.Length; j++)
            {
                if (request.Args[j].GetType() == typeof(byte[]) && request.Args[j] != null)
                {
                    request.Args[j] = MessagePackSerializer.Deserialize(methodInfo.Parameters[j].ParameterType, (byte[])request.Args[j], ContractlessStandardResolver.Options);
                }
            }
            return request;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
        }
    }
}
