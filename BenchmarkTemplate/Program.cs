using BenchmarkDotNet.Running;
using System;

namespace BenchmarkTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<CacheBenchmark>();
            Console.Read();
        }
    }
}
