using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Executor.Threads
{
    public class SingleThreadExecutor : BaseScheduler
    {
        private struct Work
        {
            public Action<object> Callback;
            public object State;
        }

        private bool _doingWork;
        private static readonly WaitCallback _doWorkCallback = s => ((SingleThreadExecutor)s).DoWork();
        private readonly object _workSync = new object();
        private readonly ConcurrentQueue<Work> _workItems = new ConcurrentQueue<Work>();

        public override void Schedule(Action<object> action, object state)
        {
            var work = new Work
            {
                Callback = action,
                State = state
            };

            _workItems.Enqueue(work);

            lock (_workSync)
            {
                if (!_doingWork)
                {
                    System.Threading.ThreadPool.UnsafeQueueUserWorkItem(_doWorkCallback, this);
                    _doingWork = true;
                }
            }
        }

        private void DoWork()
        {
            while (true)
            {
                while (_workItems.TryDequeue(out Work item))
                {
                    item.Callback(item.State);
                }

                lock (_workSync)
                {
                    if (_workItems.IsEmpty)
                    {
                        _doingWork = false;
                        return;
                    }
                }
            }
        }
    }
}
