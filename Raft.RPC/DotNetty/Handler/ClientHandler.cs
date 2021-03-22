using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;

namespace Raft.RPC.DotNetty
{
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

        /* When to invoke ChannelReadComplete?
           1.read(...) returns 0
           2.read(...) got a buffer passed to it that has 1024 bytes to fill, but less than 1024 are filled.
        */ 
        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        /// <summary>
        /// 异常处理
        /// </summary> 
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            _responseWaits.SetByChannelId(context.Channel.Id.AsLongText(), new MessageResponse() { Success = false, Error = exception });
            context.CloseAsync();
        }
    }
}