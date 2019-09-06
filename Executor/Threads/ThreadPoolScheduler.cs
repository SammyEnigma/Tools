using System;

namespace Executor.Threads
{
    public class ThreadPoolScheduler : BaseScheduler
    {
        public override void Schedule(Action<object> action, object state)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(action, state, false);
        }
    }
}
