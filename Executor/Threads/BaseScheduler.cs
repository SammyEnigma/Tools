using System;

namespace Executor.Threads
{
    public abstract class BaseScheduler : IScheduler
    {
        public static BaseScheduler ThreadPool => new ThreadPoolScheduler();

        public abstract void Schedule(Action<object> action, object state);
    }
}
