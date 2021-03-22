using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uni.Common;

namespace Uni.Core.RPC.DotNetty
{
    /// <summary>
    /// 服务端请求处理委托
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    delegate Task<IByteBuffer> InvokeDelegate(IByteBuffer request);

    /// <summary>
    /// 服务端请求处理
    /// </summary>
    class ServerHandler : ChannelHandlerAdapter
    {
        private readonly ILog _logger;
        private readonly TCPServer _server;

        /// <summary>
        /// 服务请求处理管道
        /// </summary> 
        public ServerHandler(ILog logger, TCPServer server)
        {
            _server = server;
            _logger = logger;
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buffer = message as IByteBuffer;
            IByteBuffer response = _server.InvokeAsync(buffer).Result;
            context.WriteAsync(response);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        /// <summary>
        /// 异常处理
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Dictionary<string, object> logs = new Dictionary<string, object>();
            logs.Add(TextLog.MethodNameKey, nameof(ServerHandler) + "." + nameof(ExceptionCaught));
            logs.Add(TextLog.ExceptionKey, exception);
            _logger.WriteErrorLog(logs);
            context.CloseAsync();
        }
    }
}