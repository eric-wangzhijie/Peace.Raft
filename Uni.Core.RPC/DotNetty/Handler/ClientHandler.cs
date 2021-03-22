using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;

namespace Uni.Core.RPC.DotNetty
{
    /// <summary>
    /// 反序列化消息委托
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    delegate MessageResponse DeserializeMessageDelegate(IByteBuffer buffer);

    /// <summary>
    /// 客户端请求处理
    /// </summary>
    class ClientHandler : ChannelHandlerAdapter
    {
        private readonly ResponseWaits _responseWaits;

        /// <summary>
        /// 反序列化消息委托
        /// </summary>
        public DeserializeMessageDelegate OnDeserializeMessage;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="responseWaits">响应池</param> 
        public ClientHandler(ResponseWaits responseWaits)
        {
            _responseWaits = responseWaits;
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buffer = message as IByteBuffer;
            MessageResponse response = OnDeserializeMessage(buffer);
            _responseWaits.Set(response.MessageId, response);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }

        /// <summary>
        /// 异常处理
        /// </summary> 
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            _responseWaits.SetByChannelId(context.Channel.Id.AsLongText(), 
                new MessageResponse() { Success = false, Message = exception.InnerException != null ? exception.InnerException.Message : exception?.Message });
            context.CloseAsync();
        }
    }
}