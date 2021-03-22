using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raft.Demo.StateMachine
{
    public class FileStateMachine : IStateMachine
    {
        private readonly string _id;
        private readonly string _path;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private JsonSerializerSettings _settings;

        public FileStateMachine(string nodeId)
        {
            _id = nodeId;
            _path = _id.Replace("/", "").Replace(":", "").ToString();
            _settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

            if (!File.Exists(_path))
            {
                using (FileStream fs = File.Create(_path))
                {
                    byte[] info = new UTF8Encoding().GetBytes("");
                    fs.Write(info, 0, info.Length);
                }
            }
        }

        public async Task ApplyLog(LogEntry log)
        {
            try
            {
                DebugConsole.WriteLine($"Applying log to state machine log.Term: {log.Term}");
                await _lock.WaitAsync();

                var current = await File.ReadAllTextAsync(_path);
                var logEntries = JsonConvert.DeserializeObject<List<ICommand>>(current, _settings);
                if (logEntries == null)
                {
                    logEntries = new List<ICommand>();
                }

                logEntries.Add(log.Command);
                var next = JsonConvert.SerializeObject(logEntries, _settings);
                await File.WriteAllTextAsync(_path, next);
            }
            catch (Exception e)
            {
                DebugConsole.WriteLine($"There is an exception when trying to handle log entry, exception: {e}");
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
