using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 客户端基类
    /// </summary>
    internal abstract class AbstractClient<TProtocol> : DynamicObject, IClient 
    {
        /// <summary>
        /// 请求超时设置
        /// </summary>
        protected const int RequestTimeoutSeconds = 60;

        private const int FailedConnectionRetryIntervalSeconds = 10;
        private readonly ClientStub _clientStub;
        private readonly ServiceReflectionInfo _serviceInfo;

        /// <summary>
        /// 构造函数
        /// </summary> 
        /// <param name="clientStub"></param>
        /// <param name="serviceType"></param>
        internal AbstractClient(ClientStub clientStub, Type serviceType)
        {
            _clientStub = clientStub;
            _serviceInfo = _clientStub.GetOrSetServiceInfo(serviceType);
        }

        /// <summary>
        /// 序列化消息请求体
        /// </summary>
        /// <param name="obj">消息请求体</param>
        /// <returns>消息协议泛型</returns>
        protected abstract TProtocol SerializeMessage(MessageRequest obj);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="input">序列化后的消息体</param>
        /// <returns>消息响应体</returns>
        protected abstract MessageResponse DeserializeMessage(TProtocol input);

        /// <summary>
        /// 请求调用
        /// </summary>
        /// <param name="messageId">消息Id</param>
        /// <param name="message">序列化后的消息体</param>
        /// <param name="endPoint">远程服务端地址</param> 
        /// <returns></returns>
        protected abstract MessageResponse Invoke(string messageId, TProtocol message, IPEndPoint endPoint);

        /// <summary>
        /// 调用入口
        /// </summary>
        /// <param name="binder">请求的绑定对象</param>
        /// <param name="args">参数集合</param>
        /// <param name="result">返回结果</param>
        /// <returns></returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            //处理参数  
            MethodReflectionInfo methodInfo = _serviceInfo.EnsureMethodInfo(binder.Name, args, null, binder.CallInfo.ArgumentCount);

            //序列化消息数据
            bool isExisted = _clientStub.TraceChain.GetCurrentTraceContext() != null;
            var request = new MessageRequest
            {
                MessageId = Guid.NewGuid().ToString(),
                TraceContext = isExisted ? _clientStub.TraceChain.GetCurrentTraceContext() : _clientStub.TraceChain.Begin(),
                Service = _serviceInfo.ServiceType.Name,
                Method = binder.Name,
                ClusterToken = _clientStub.ClusterToken,
                ArgTypes = methodInfo.Parameters.Any() ? new List<string>() : null,
                Args = methodInfo.Parameters.Any() ? new List<object>() : null
            };

            for (int i = 0; i < methodInfo.Parameters.Length; i++)
            {
                request.ArgTypes.Add(methodInfo.Parameters[i].ParameterType.AssemblyQualifiedName); //todo 但继承的子类不在同个程序集的时候无法反射
                request.Args.Add(args[i]);
            }

            TProtocol message = SerializeMessage(request);
            result = null;
            if (_clientStub.EnableServiceDiscovery)
            {
                bool success = InvokeTolerably(request.MessageId, message, out Exception lastException, out object ret);
                if (!success && lastException != null)
                {
                    throw lastException;
                }
                result = ret;
            }
            else
            {
                MessageResponse response = Invoke(request.MessageId, message, _clientStub.EndPoint);
                result = response.Data;
            }

            if (methodInfo.IsAwaitable) //处理异步返回值
            {
                if (methodInfo.ReturnType.IsGenericType)
                {
                    Type genericParamType = methodInfo.Method.ReturnType.GenericTypeArguments.First();
                    dynamic value = Utility.ChangeType(result, genericParamType);
                    if (value == null)
                    {
                        result = Utility.GetTaskByType(genericParamType);
                    }
                    else
                    {
                        result = Task.FromResult(value);
                    }
                }
                else
                {
                    result = Task.CompletedTask;
                }
            }

            //完成链路
            if (!isExisted)
            {
                _clientStub.TraceChain.Finish();
            }
            return true;
        }

        private bool InvokeTolerably(string messageId, TProtocol message, out Exception lastException, out object ret)
        {
            ret = default;
            lastException = null;

            string service = _serviceInfo.ServiceType.Name;
            int total = _clientStub.GetConnectionCount(service);
            if (total == 0)
            {
                lastException = new ServiceNotFoundException($"No any available address for {service}");
            }

            for (int i = 0; i < total; i++)
            {
                ClientConnection conn = null;
                try
                {
                    conn = _clientStub.EnsureConnection(service);
                    if (conn != null)
                    {
                        // 检查是否过了重试时间
                        if (!conn.Available && DateTime.Now.Subtract(conn.LastFailedTime).TotalSeconds < FailedConnectionRetryIntervalSeconds)
                        {
                            continue;
                        }
                        //等待返回
                        MessageResponse response = Invoke(messageId, message, new IPEndPoint(IPAddress.Parse(conn.Host), conn.Port));
                        ret = response.Data;
                        conn.Available = true;
                        lastException = null;
                        return true;
                    }
                }
                catch (AggregateException ae)
                {
                    Exception deepestException = GetDeepestInnerException(ae);
                    if (deepestException is SocketException) //连接失败进行重试
                    {
                        lastException = new RpcConnectException($"{deepestException.Message} [Target: {conn.Host}:{conn.Port}]", deepestException);
                        conn.Available = false;
                        conn.LastFailedTime = DateTime.Now;
                    }
                    else
                    {
                        lastException = ae.InnerException;
                    }
                }
                catch
                {
                    throw;
                }
            }
            return false;
        }

        private Exception GetDeepestInnerException(Exception root)
        {
            if (root.InnerException == null)
            {
                return root;
            }
            return GetDeepestInnerException(root.InnerException);
        } 
    }
}
