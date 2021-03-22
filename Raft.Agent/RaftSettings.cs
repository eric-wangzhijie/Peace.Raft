using Microsoft.Extensions.Configuration;
using System.IO;

namespace Raft.Agent
{
    public class RaftSettings
    {
        public string NodeId { get; set; }

        public string NodeName { get; set; }

        public string[] Addresses { get; set; }

        public int RetryCount { get; set; }

        private static RaftSettings _settings;

        /// <summary>
        /// 读取配置文件[AppSettings]节点数据
        /// </summary>
        public static RaftSettings Items
        {
            get
            {
                if (_settings == null)
                {
                    IConfiguration Configuration = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json")
                  .Build();

                    _settings = new RaftSettings();
                    Configuration.GetSection(typeof(RaftSettings).Name).Bind(_settings);
                }
                return _settings;
            }
        }
    }
}
