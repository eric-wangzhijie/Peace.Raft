using System.Collections.Generic;

namespace Raft.RPC
{
    /// <summary>
    /// 请求消息体
    /// </summary>
    [System.Serializable]
    public class MessageRequest : MessageBase
    { 
        /// <summary>
        /// 授权token
        /// </summary>
        public string ClusterToken { get; set; }

        /// <summary>
        /// 请求的服务
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// 请求的方法
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 参数类型集合
        /// </summary>
        public List<string> ArgTypes { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary> 
        public List<object> Args { get; set; }
    }
}
