using System;
using System.Threading.Tasks;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 服务端接口类
    /// </summary> 
    public interface IServer : IDisposable
    {
        /// <summary>
        /// 启动服务
        /// </summary>
        Task Start();

        /// <summary>
        /// 停止服务
        /// </summary>
        Task Stop();

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="IService">服务接口</typeparam>
        /// <typeparam name="Service">服务类型</typeparam>
        IServer Register<IService, Service>() where Service : ServiceBase, IService where IService : class;

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="serviceInterfaceType">服务接口</param>
        /// <param name="serviceType">服务类型</param>
        /// <param name="serviceInstance">服务实例</param> 
        IServer Register(Type serviceInterfaceType, Type serviceType, object serviceInstance);
    }
}