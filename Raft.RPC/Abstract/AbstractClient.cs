using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Raft.RPC
{
    /// <summary>
    /// 客户端基类
    /// </summary>
    internal abstract class AbstractClient<TProtocol> : DynamicObject, IClient
    {  
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
            var request = new MessageRequest
            {
                MessageId = Guid.NewGuid().ToString(),
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
            MessageResponse response = Invoke(request.MessageId, message, _clientStub.EndPoint);
            result = response.Data;

            if (methodInfo.IsAwaitable) //处理异步返回值
            {
                if (methodInfo.ReturnType.IsGenericType)
                {
                    Type genericParamType = methodInfo.Method.ReturnType.GenericTypeArguments.First();
                    dynamic value = Utility.ChangeType(result, genericParamType); 
                    result = Utility.GetTaskByType(genericParamType, value);
                }
                else
                {
                    result = Task.CompletedTask;
                }
            }

            return true;
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
