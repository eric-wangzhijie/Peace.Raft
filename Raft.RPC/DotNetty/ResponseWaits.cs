using System.Collections.Concurrent;
using System.Threading;

namespace Raft.RPC.DotNetty
{
    /// <summary>
    /// 可等待的响应集合
    /// </summary>
    class ResponseWaits
    {
        /// <summary>
        /// 当前客户端响应消息池 [MessageId /MessageResponse]
        /// </summary>
        private ConcurrentDictionary<string, ResponseWait> _waits = new ConcurrentDictionary<string, ResponseWait>();

        /// <summary>
        /// 用来处理异常的消息id和信道id映射[ChannelId /MessageId]
        /// </summary>
        private ConcurrentDictionary<string, string> _messageChannelMap = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 添加响应
        /// </summary>
        /// <param name="messageId">消息id</param>
        /// <param name="channelId">信道id</param>
        public void Add(string messageId, string channelId)
        {
            _waits[messageId] = new ResponseWait() { ChannelId = channelId };
            _messageChannelMap[channelId] = messageId;
        }

        /// <summary>
        /// 设置等待响应状态
        /// </summary>
        /// <param name="messageId">消息id</param>
        /// <param name="response">响应实体</param>
        public void Set(string messageId, MessageResponse response)
        {
            ResponseWait wait = _waits[messageId];
            wait.Response = response;
            wait.Set();
        }

        /// <summary>
        /// 设置等待响应状态
        /// </summary> 
        /// <param name="channelId">信道Id</param>
        /// <param name="response">响应实体</param>
        public void SetByChannelId(string channelId, MessageResponse response)
        {
            string messageId = _messageChannelMap[channelId];
            ResponseWait wait = _waits[messageId];
            response.MessageId = messageId;
            wait.Response = response;
            wait.Set();
        }

        /// <summary>
        /// 等待响应
        /// </summary>
        /// <param name="messageId">消息id</param>
        /// <returns></returns>
        public ResponseWait Wait(string messageId)
        {
            ResponseWait wait = _waits[messageId];
            wait.Wait();
            _messageChannelMap.TryRemove(wait.ChannelId, out _);
            _waits.TryRemove(messageId, out _);
            return wait;
        }
    }

    /// <summary>
    /// 可等待的响应
    /// </summary>
    class ResponseWait
    {
        private readonly AutoResetEvent autoReset = new AutoResetEvent(false);

        /// <summary>
        /// 消息体
        /// </summary>
        public MessageResponse Response;

        /// <summary>
        /// 信道Id
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// 设置信号状态
        /// </summary>
        public void Set()
        {
            autoReset.Set();
        }

        /// <summary>
        /// 阻塞当前线程
        /// </summary>
        public void Wait()
        {
            autoReset.WaitOne(60 * 1000);
        }
    }
}
