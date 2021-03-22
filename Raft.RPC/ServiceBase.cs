namespace Raft.RPC
{
    /// <summary>
    /// 服务基类
    /// </summary>
    public abstract class ServiceBase
    {
        /// <summary>
        /// 服务存根
        /// </summary>
        public ServerStub ServerStub { get; set; }
    }
}
