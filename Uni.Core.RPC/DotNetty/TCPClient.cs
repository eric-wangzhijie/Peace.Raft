using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Net;

namespace Uni.Core.RPC.DotNetty
{
    /// <summary>
    /// DotNetty客户端
    /// </summary>
    internal class TCPClient : AbstractClient<IByteBuffer>
    {
        private readonly Bootstrap _bootstrap;
        private readonly ResponseWaits _responseWaits;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="clientStub"></param>
        /// <param name="serviceType"></param> 
        internal TCPClient(ClientStub clientStub, Type serviceType) : base(clientStub, serviceType)
        {
            _responseWaits = new ResponseWaits(RequestTimeoutSeconds);
            _bootstrap = new Bootstrap()
             .Group(new MultithreadEventLoopGroup())
             .Channel<TcpSocketChannel>()
             .Option(ChannelOption.TcpNodelay, true)
             .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
             {
                 IChannelPipeline pipeline = channel.Pipeline;
                 pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                 //数据包最大长度
                 pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                 ClientHandler clientHandler = new ClientHandler(_responseWaits)
                 {
                     OnDeserializeMessage = DeserializeMessage
                 };
                 pipeline.AddLast(clientHandler);
             }));
        }

        /// <summary>
        /// 序列化消息请求体
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>消息协议泛型</returns>
        protected override IByteBuffer SerializeMessage(MessageRequest obj)
        {
            //参数数据先转换成二进制
            List<object> list = new List<object>();
            for (int i = 0; i < obj.ArgTypes.Count; i++)
            {
                list.Add(MessagePackSerializer.Serialize(obj.Args[i], MessagePack.Resolvers.ContractlessStandardResolver.Options));
            }
            obj.Args = list;

            byte[] data = MessagePackSerializer.Serialize(obj, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            if (data.Length > ushort.MaxValue)
            {
                throw new RpcInternalException("The message length is up to maximum of Dotnetty：" + ushort.MaxValue);
            }
            return Unpooled.WrappedBuffer(data);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>消息响应体</returns>
        protected override MessageResponse DeserializeMessage(IByteBuffer buffer)
        {
            var data = new byte[buffer.MaxCapacity];
            buffer.ReadBytes(data);
            MessageResponse ret = MessagePackSerializer.Deserialize<MessageResponse>(data, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            if (!string.IsNullOrWhiteSpace(ret.ReturnType))
            {
                ret.Data = MessagePackSerializer.Deserialize(Type.GetType(ret.ReturnType), (byte[])ret.Data, MessagePack.Resolvers.ContractlessStandardResolver.Options);
            }
            return ret;
        }

        /// <summary>
        /// 请求调用
        /// </summary>
        /// <param name="messageId">消息Id</param>
        /// <param name="message">序列化后的消息体</param>
        /// <param name="endPoint">远程服务地址</param> 
        /// <returns></returns>
        protected override MessageResponse Invoke(string messageId, IByteBuffer message, IPEndPoint endPoint)
        {
            IChannel clientChannel = _bootstrap.ConnectAsync(endPoint).Result;

            _responseWaits.Add(messageId, clientChannel.Id.AsLongText());
            clientChannel.WriteAndFlushAsync(message);
            var response = _responseWaits.Wait(messageId).Response;

            clientChannel.CloseAsync();
            if (response == null)
            {
                throw new RpcInternalException($"Request is timeout({RequestTimeoutSeconds}s).");
            }
            else if (!response.Success)
            {
                throw new Exception(response.Message);
            }
            return response;
        } 
    }
}
