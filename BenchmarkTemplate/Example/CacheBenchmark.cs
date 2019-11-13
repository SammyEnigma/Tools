using BenchmarkDotNet.Attributes;
using System.Threading.Tasks;

namespace BenchmarkTemplate
{
    [MemoryDiagnoser]
    [HtmlExporter]
    public class CacheBenchmark
    {
        [Params(10000, 10 * 0000)]
        public int Count { get; set; }

        [Params(1, 2, 37)]
        public int OpType { get; set; }

        [Params(6, 8, 12)]
        public int MaxDegreeOfParallelism { get; set; }

        private FixedThreadPoolScheduler _wokerScheduler { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // init cache here...
            _wokerScheduler = new FixedThreadPoolScheduler(MaxDegreeOfParallelism);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // dispose here...
            _wokerScheduler.Dispose();
        }

        [Benchmark]
        public async Task PerfOfXXX()
        {
            var itemsCount = Count / MaxDegreeOfParallelism;
            var factory = new WorkerFactory((ICache)new object(), MaxDegreeOfParallelism, OpType, itemsCount, _wokerScheduler);
            await factory.StartWorkersAsync();
        }
    }
}
