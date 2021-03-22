using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;

namespace Raft.RPC.DotNetty
{
    /// <summary>
    /// 服务端请求处理委托
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    delegate IByteBuffer InvokeDelegate(IByteBuffer request);

    /// <summary>
    /// 服务端请求处理
    /// </summary>
    class ServerHandler : ChannelHandlerAdapter
    {
        /// <summary>
        /// 调用委托
        /// </summary>
        public InvokeDelegate OnInvoke;

        private readonly ILog _logger;

        /// <summary>
        /// 服务请求处理管道
        /// </summary> 
        public ServerHandler(ILog logger)
        {
            _logger = logger;
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        { 
            var buffer = message as IByteBuffer;
            IByteBuffer response = OnInvoke(buffer);
            context.WriteAsync(response);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        /// <summary>
        /// 异常处理
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            _logger.WriteErrorLog(exception);
            context.CloseAsync();
        }
    }
}