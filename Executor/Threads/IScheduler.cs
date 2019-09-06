using System;

namespace Executor.Threads
{
    internal interface IScheduler
    {
        void Schedule(Action<object> action, object state);
    }
}
