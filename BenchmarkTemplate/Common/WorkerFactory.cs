using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkTemplate
{
    public class WorkerFactory
    {
        private readonly int _opType;
        private readonly int _itemsCount;
        private readonly int _workerCount;
        private readonly ICache _cache;
        private readonly TaskScheduler _scheduler;

        public WorkerFactory(ICache cache,
            int workerCount,
            int opType,
            int itemsCount, TaskScheduler scheduler)
        {
            _cache = cache;
            _opType = opType;
            _workerCount = workerCount;
            _itemsCount = itemsCount;
            _scheduler = scheduler;
        }

        public async Task StartWorkersAsync()
        {
            var workers = new Task[_workerCount];
            for (var i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Factory.StartNew(
                    () => new Worker(_cache, _opType, _itemsCount).Start(),
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    _scheduler
                );
            }

            await Task.WhenAll(workers);
        }
    }
}
