using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Raft.RPC
{
    /// <summary>
    /// 抽象的服务基类
    /// </summary>
    /// <typeparam name="TProtocol">通讯协议类型</typeparam>
    public abstract class AbstractServer<TProtocol> : IServer
    { 
        private readonly ServerStub _serverStub;
        private readonly string _clusterToken;
        private readonly ILog _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host">主机ip地址</param>
        /// <param name="port">主机端口</param>
        /// <param name="clusterToken">集群授权码</param>
        /// <param name="logger">日志</param>
        /// 
        public AbstractServer(string host, int port, string clusterToken, ILog logger)
        { 
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentNullException(nameof(host));
            }
            if (string.IsNullOrEmpty(clusterToken))
            {
                throw new ArgumentNullException(nameof(clusterToken));
            }
            if (port < 0 || port > ushort.MaxValue)
            {
                throw new ArgumentException(nameof(port));
            } 
            _logger = logger;
            _serverStub = new ServerStub(host, port, logger);
            _clusterToken = clusterToken;
        }

        /// <summary>
        /// 启动服务核心业务逻辑
        /// </summary>
        protected abstract Task StartCore();

        /// <summary>
        /// 启动服务
        /// </summary>
        public async Task Start()
        {
            await StartCore();
            var serviceNames = _serverStub.GetAllServiceNames();
            if (serviceNames != null)
            { 
            }
            else
            {
                throw new RpcInternalException($"No any logic service is found.");
            }
        }

        private const int RenewIntervalSeconds = 5;
        private DateTime _renewTime = DateTime.Now;

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="input">待序列化消息</param>
        /// <returns>消息响应体</returns>
        protected abstract TProtocol SerializeMessage(MessageResponse input);

        /// <summary>
        /// 序列化消息请求体
        /// </summary>
        /// <param name="buffer">序列化消息</param>
        /// <returns>消息协议泛型</returns>
        protected abstract MessageRequest DeserializeMessage(TProtocol buffer);

        /// <summary>
        /// 请求调用
        /// </summary>
        /// <param name="buffer">已序列化的消息请求</param>
        /// <returns>序列化消息</returns>
        public async Task<TProtocol> InvokeAsync(TProtocol buffer)
        {
            MessageResponse response = new MessageResponse();
            try
            {
                Dictionary<string, object> logs = new Dictionary<string, object>();
                MessageRequest request = DeserializeMessage(buffer);
                try
                {
                    if (request.ClusterToken != _clusterToken)
                    {
                        throw new IllegalClusterTokenException($"Cluster token({request.ClusterToken}) is illegal.");
                    }

                    MethodReflectionInfo methodInfo = EnsureMethodInfo(request);
                    var serviceInstance = _serverStub.GetServiceInstance(request.Service);
                    object result = null;
                    object ret = methodInfo.Method.Invoke(serviceInstance, request.Args?.ToArray());

                    if (methodInfo.IsAwaitable) //处理异步
                    {
                        dynamic task = (Task)ret;
                        if (methodInfo.ReturnType.IsGenericType)
                        {
                            result = await task.ConfigureAwait(false);
                        }
                        else
                        {
                            await task.ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        result = ret;
                    }

                    response.MessageId = request.MessageId;
                    response.Data = result;
                    response.ReturnType = result?.GetType()?.AssemblyQualifiedName;
                    response.Success = true;
                    return SerializeMessage(response);
                }
                catch (Exception ex)
                {
                    _logger.WriteErrorLog(ex);
                    throw;
                }
                finally
                {
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _logger.WriteErrorLog(ex);
            }
            return SerializeMessage(response);
        }

        /// <summary>
        /// 获取服务方法反射信息
        /// </summary>
        /// <param name="request">请求消息</param>
        /// <returns>方法反射信息</returns>
        protected MethodReflectionInfo EnsureMethodInfo(MessageRequest request)
        {
            var serviceInfo = _serverStub.GetServiceInfo(request.Service);
            if (serviceInfo == null)
            {
                throw new RpcInternalException($"The service {request.Service} is not found.");
            }
            List<Type> types = new List<Type>();
            for (int i = 0; i < request.ArgTypes?.Count; i++)
            {
                types.Add(Type.GetType(request.ArgTypes[i]));
            }
            MethodReflectionInfo methodInfo = serviceInfo.EnsureMethodInfo(request.Method, request.Args?.ToArray(), types.ToArray(), request.Args?.Count ?? 0);

            if (methodInfo == null)
            {
                throw new RpcInternalException($"The parameters of service method {request.Method} is unmatch. Parameters:{(request.ArgTypes == null ? "" : string.Join("; ", request.ArgTypes))}");
            }

            return methodInfo;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="IService">服务接口</typeparam>
        /// <typeparam name="Service">服务类型</typeparam>
        public IServer Register<IService, Service>() where Service : ServiceBase, IService where IService : class
        {
            var serviceType = typeof(Service);
            Type serviceInterfaceType = typeof(IService);
            var serviceInfo = _serverStub.GetServiceInfo(serviceInterfaceType.Name);
            if (serviceInfo == null)
            {
                _serverStub.AddServiceInfo(serviceInterfaceType);
                _serverStub.AddServiceInstance(serviceInterfaceType, serviceType);
            }
            else
            {
                throw new RpcInternalException($"The logic service '{serviceType.Name}' is already registed.");
            }
            return this;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="serviceInterfaceType">服务接口</param>
        /// <param name="serviceType">服务类型</param>
        /// <param name="serviceInstance">服务实例</param> 
        public IServer Register(Type serviceInterfaceType, Type serviceType, object serviceInstance)
        {
            var serviceInfo = _serverStub.GetServiceInfo(serviceInterfaceType.Name);
            if (serviceInfo == null)
            {
                _serverStub.AddServiceInfo(serviceInterfaceType);
                if (serviceInstance != null)
                {
                    _serverStub.AddServiceInstance(serviceInterfaceType, serviceType, serviceInstance as ServiceBase);
                }
                else
                {
                    _serverStub.AddServiceInstance(serviceInterfaceType, serviceType);
                }
            }
            else
            {
                throw new RpcInternalException($"The logic service '{serviceType.Name}' is already registed.");
            }
            return this;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}