using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Raft.RPC.DotNetty
{
    /// <summary>
    /// dotNetty服务端
    /// </summary>
    public class TCPServer : AbstractServer<IByteBuffer>, IDisposable
    {
        private readonly ServerBootstrap _serverBootstrap;
        private IChannel _channel;
        private readonly string _host;
        private readonly int _port;
        private readonly ILog _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="clusterToken"></param>
        /// <param name="logger"></param>
        public TCPServer(string host, int port, string clusterToken, ILog logger) : base(host, port, clusterToken, logger)
        {
            _host = host;
            _port = port;
            _logger = logger;
            MultithreadEventLoopGroup bossGroup = new MultithreadEventLoopGroup();
            MultithreadEventLoopGroup workerGroup = new MultithreadEventLoopGroup();
            _serverBootstrap = new ServerBootstrap()
            .Group(bossGroup, workerGroup)
            .Channel<TcpServerSocketChannel>()
            .Option(ChannelOption.SoBacklog, 1024)
            .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
            {
                IChannelPipeline pipeline = channel.Pipeline;
                pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                //数据包最大长度
                pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                ServerHandler serverHandler = new ServerHandler(_logger)
                {
                    OnInvoke = Invoke
                };
                pipeline.AddLast(serverHandler);
            }));
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public override async Task Start()
        {
            _channel = await _serverBootstrap.BindAsync(new IPEndPoint(IPAddress.Parse(_host), _port));
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="response"></param>
        /// <returns>消息响应体</returns>
        protected override IByteBuffer SerializeMessage(MessageResponse response)
        {
            byte[] data = Utility.ObjectToBytes(response);
            if (data.Length > ushort.MaxValue)
            {
                throw new RpcInternalException("The message length is up to maximum of Dotnetty：" + ushort.MaxValue);
            }
            return Unpooled.WrappedBuffer(data);
        }

        /// <summary>
        /// 序列化消息请求体
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>消息协议泛型</returns>
        protected override MessageRequest DeserializeMessage(IByteBuffer buffer)
        {
            var data = new byte[buffer.MaxCapacity];
            buffer.ReadBytes(data);
            return (MessageRequest)Utility.BytesToObject(data);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_channel != null)
            {
                _channel.CloseAsync();
            }
        }
    }
}
