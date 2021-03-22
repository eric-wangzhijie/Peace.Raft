using System;
using System.Threading;
using System.Threading.Tasks;

namespace Raft.Demo
{
    public class ElectionAlarm
    {
        private const int MinValue = 1500;
        private const int MaxValue = 3000;

        private readonly Random _rd = new Random((int)DateTime.Now.Ticks);
        private int _electionTimeoutMs = 0;
        private bool _canStop = false;

        public void StartBeforeTimewait(Action action)
        {
            Start(action, 0, true, true);
        }

        public void StartAfterTimewait(Action action)
        {
            Start(action, 0, true, false);
        }

        public void Start(Action action, int intervalMilliseconds = 0, bool enabledElectionTimeout = false, bool isBefore = false)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    for (; ; )
                    {
                        if (_canStop)
                        {
                            return;
                        }

                        if (enabledElectionTimeout)
                        {
                            _electionTimeoutMs = _rd.Next(MinValue, MaxValue);
                        }

                        if (isBefore)
                        {
                            Thread.Sleep(intervalMilliseconds + _electionTimeoutMs);
                        }

                        action();

                        if (!isBefore)
                        {
                            Thread.Sleep(intervalMilliseconds + _electionTimeoutMs);
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugConsole.WriteLine(e.Message);
                }

            }, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _canStop = true;
        }
    }
}
