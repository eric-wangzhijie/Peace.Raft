using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Raft.RPC.DotNetty
{
    /// <summary>
    /// 反序列化消息委托
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    delegate MessageResponse DeserializeMessageDelegate(IByteBuffer buffer);

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
            _responseWaits = new ResponseWaits();
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
            byte[] data = Utility.ObjectToBytes(obj);
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
            return (MessageResponse)Utility.BytesToObject(data);
        }
         
        /// <summary>
        /// 请求调用
        /// </summary>
        /// <param name="context">调用上下文</param>
        /// <param name="message">序列化的消息体</param>
        /// <returns></returns>
        protected override async Task<MessageResponse> Invoke(InvokeContext context, IByteBuffer message)
        {
            IChannel clientChannel = await _bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(context.Host), context.Port));
             
            _responseWaits.Add(context.MessageId, clientChannel.Id.AsLongText());
            await clientChannel.WriteAndFlushAsync(message); 

            var sw = Stopwatch.StartNew();
            var response = _responseWaits.Wait(context.MessageId).Response;
            if (sw.ElapsedMilliseconds > 1 * 1000)
            {
                Console.WriteLine($"Invoke cost: {sw.ElapsedMilliseconds}...");
            }

            await clientChannel.CloseAsync();
            if (response == null)
            {
                throw new RpcInternalException("Request is timeout");
            }
            if (!response.Success && response.Error != null)
            {
                throw response.Error;
            }
            return response;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
        }
    }
}
