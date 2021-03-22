using Grpc.Core;
using MessagePack;
using Raft.RPC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace Raft.RPC.Grpc
{
    /// <summary>
    /// DotNetty客户端
    /// </summary>
    internal class GrpcClient : AbstractClient<byte[]>
    { 
        /// <summary>
        /// [services / refresh time]
        /// </summary>
        private readonly ConcurrentDictionary<string, Channel> _chancelDic = new ConcurrentDictionary<string, Channel>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="clientStub"></param>
        /// <param name="serviceType"></param> 
        internal GrpcClient(ClientStub clientStub, Type serviceType) : base(clientStub, serviceType)
        {
        }

        /// <summary>
        /// 请求调用
        /// </summary>
        /// <param name="messageId">消息Id</param>
        /// <param name="message">序列化后的消息体</param>
        /// <param name="endPoint">远程服务地址</param> 
        /// <returns></returns>
        protected override MessageResponse Invoke(string messageId, byte[] message, IPEndPoint endPoint)
        {
            string address = endPoint.Address.ToString() + ":" + endPoint.Port;
            if (!_chancelDic.ContainsKey(address))
            {
                _chancelDic.TryAdd(address, new Channel(endPoint.Address.ToString(), endPoint.Port, SslCredentials.Insecure));
            }
            GlueServiceClient client = new GlueServiceClient(_chancelDic[address]);
            byte[] response = client.Invoke(message, null, null).GetAwaiter().GetResult();
            return DeserializeMessage(response);
        }

        /// <summary>
        /// 序列化数据
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override byte[] SerializeMessage(MessageRequest obj)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < obj.ArgTypes.Count; i++)
            {
                list.Add(MessagePackSerializer.Serialize(obj.Args[i], MessagePack.Resolvers.ContractlessStandardResolver.Options));
            }
            obj.Args = list;

            return MessagePackSerializer.Serialize(obj, MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        /// <summary>
        /// 反序列化数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        protected override MessageResponse DeserializeMessage(byte[] input)
        {
            MessageResponse ret = MessagePackSerializer.Deserialize<MessageResponse>(input, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            if (!string.IsNullOrEmpty(ret.ReturnType))
            {
                ret.Data = MessagePackSerializer.Deserialize(Type.GetType(ret.ReturnType), (byte[])ret.Data, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            }
            return ret;
        }
    }
}
