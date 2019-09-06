using Executor.Example;
using System;

namespace Executor_sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var importer = new QueueImporter(false, "queue");
            importer.Run();

            Console.ReadKey();
        }
    }
}
