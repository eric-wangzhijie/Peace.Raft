using Raft.RPC;
using System;

namespace Raft.Demo
{
    public class DebugConsole : ILog
    {
        public static void WriteLine(string message)
        {
            System.Console.WriteLine($"{DateTime.Now}: {message}");
        }

        public void WriteErrorLog(Exception exception)
        {
            System.Console.WriteLine($"{DateTime.Now}: {exception}");
        }
    }
}
