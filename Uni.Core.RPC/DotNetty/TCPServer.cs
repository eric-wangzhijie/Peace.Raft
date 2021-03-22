using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using MessagePack;
using MessagePack.Resolvers;
using System.Net;
using System.Threading.Tasks;
using Uni.Common;

namespace Uni.Core.RPC.DotNetty
{
    /// <summary>
    /// dotNetty服务端
    /// </summary>
    public class TCPServer : AbstractServer<IByteBuffer>
    {
        private readonly ServerBootstrap _serverBootstrap;
        private IChannel _channel;
        private readonly string _host;
        private readonly int _port;
        private readonly ILog _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// /// <param name="id">服务实例的id，目前每个服务仅允许有一个服务实例</param>
        /// <param name="host">主机ip地址</param>
        /// <param name="port">主机端口</param>
        /// <param name="clusterToken">集群授权码</param>
        /// <param name="logger">日志</param>
        /// <param name="redisConnection">注册中心redis连接地址</param>
        public TCPServer(string id, string host, int port, string clusterToken, ILog logger, string redisConnection)
            : base(id, host, port, clusterToken, logger, redisConnection)
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

                ServerHandler serverHandler = new ServerHandler(_logger, this);
                pipeline.AddLast(serverHandler);
            }));
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        protected override async Task StartCore()
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
            if (response.Data != null)
            {
                response.Data = MessagePackSerializer.Serialize(response.Data, ContractlessStandardResolver.Options);
            }
            byte[] data = MessagePackSerializer.Serialize(response, ContractlessStandardResolver.Options);
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
            var request = MessagePackSerializer.Deserialize<MessageRequest>(data, ContractlessStandardResolver.Options);
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
            if (_channel != null)
            {
                _channel.CloseAsync();
            }
        }
    }
}
