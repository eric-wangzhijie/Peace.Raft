using System;

namespace Raft.RPC
{
    /// <summary>
    /// 服务端接口类
    /// </summary> 
    public interface IServer
    {
        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="IService">服务接口</typeparam>
        /// <typeparam name="Service">服务类型</typeparam>
        IServer Register<IService, Service>() where Service : ServiceBase, IService where IService : class;
    }
}