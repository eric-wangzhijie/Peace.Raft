namespace Raft.RPC
{
    /// <summary>
    /// 消息体基类
    /// </summary>
    [System.Serializable]
    public abstract class MessageBase
    {
        /// <summary>
        /// 消息Id
        /// </summary>
        public string MessageId { get; set; }
    }
}