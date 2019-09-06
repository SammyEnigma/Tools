using Executor.Threads;
using System.Collections.Generic;

namespace Executor
{
    public abstract class ExecuteNode
    {
        public string Name;
        protected BaseScheduler _scheduler;
        public ExecuteNode(bool concurrency)
        {
            if (concurrency)
                _scheduler = BaseScheduler.ThreadPool;
            else
                _scheduler = new SingleThreadExecutor();
        }

        public abstract void Run();
        public abstract void PostItem(object item);
        public abstract void PostItems(IList<object> items);
    }
}
