using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uni.Common;

namespace Uni.Core.RPC
{
    /// <summary>
    /// 存根
    /// </summary>
    public abstract class StubBase
    {
        private readonly Dictionary<string, ServiceReflectionInfo> _serviceInfos = new Dictionary<string, ServiceReflectionInfo>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志</param> 
        public StubBase(ILog logger)
        {
            Functions = new Dictionary<uint, Func<Task>>();
            Logger = logger;
            Thread thread = new Thread(async () => { await Deamon(); })
            {
                IsBackground = true,
                Name = "StubBase"
            };
            thread.Start();
        }

        /// <summary>
        /// 日志
        /// </summary>
        public ILog Logger { get; } 

        /// <summary>
        /// 后台执行的委托,会开一个线程去处理，函数的优先级由key决定，key越大越先执行
        /// </summary>
        public Dictionary<uint, Func<Task>> Functions { get; }

        /// <summary>
        /// 获取服务信息
        /// </summary>
        /// <param name="type">服务类型</param>
        /// <returns>服务信息</returns>
        public void AddServiceInfo(Type type)
        {
            if (_serviceInfos.ContainsKey(type.Name))
            {
                throw new RpcInternalException($"The type {type} has registed.");
            }
            _serviceInfos.Add(type.Name, ServiceReflectionInfo.GetServiceInfo(type));
        }

        /// <summary>
        /// 获取服务信息
        /// </summary>
        /// <param name="typeName">服务类型</param>
        /// <returns>服务信息</returns>
        public ServiceReflectionInfo GetServiceInfo(string typeName)
        {
            if (_serviceInfos.ContainsKey(typeName))
            {
                return _serviceInfos[typeName];
            }
            return null;
        }

        /// <summary>
        /// 获取服务信息
        /// </summary>
        /// <param name="type">服务类型</param>
        /// <returns>服务信息</returns>
        public ServiceReflectionInfo GetOrSetServiceInfo(Type type)
        {
            if (!_serviceInfos.ContainsKey(type.Name))
            {
                AddServiceInfo(type);
            }
            return _serviceInfos[type.Name];
        }

        /// <summary>
        /// 获取服务信息
        /// </summary> 
        /// <returns>服务信息</returns>
        public List<string> GetAllServiceNames()
        {
            if (_serviceInfos.Any())
            {
                return _serviceInfos.Select(s => s.Key).ToList();
            }
            return null;
        }

        private async Task Deamon()
        {
            while (true)
            {
                try
                {
                    if (Functions.Any())
                    {
                        foreach (var kv in Functions.OrderByDescending(o => o.Key))
                        {
                            await kv.Value();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorLog(ex);
                }
                await Task.Delay(1000);
            }
        }
    }
}