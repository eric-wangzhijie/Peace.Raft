namespace Raft.RPC
{
    /// <summary>
    /// 调用上下文
    /// </summary>
    public class InvokeContext
    {
        /// <summary>
        /// 消息id
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// 请求服务端主机ip
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 请求服务端端口
        /// </summary>
        public int Port { get; set; }
    }
}
