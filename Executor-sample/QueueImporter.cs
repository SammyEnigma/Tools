using System;
using System.Collections.Generic;
using System.Threading;

namespace Executor.Example
{
    public class QueueImporter : Importer
    {
        Thread _thread;
        public QueueImporter(bool concurrency, string scriptPath)
            : base(concurrency, scriptPath)
        { }

        public override void LoadConfig()
        {
            OnConfigLoad();
            //...
            OnConfigLoaded();
        }

        public override void Run()
        {
            _thread = new Thread(() =>
            {
                while (true)
                {
                    var msg = new object();
                    _scheduler.Schedule((_) =>
                    {
                        Dispatch(msg);
                    },
                    this);
                    Thread.Sleep(1000);
                }
            });
            _thread.Start();
        }

        public override void PostItem(object item) => throw new NotImplementedException();

        public override void PostItems(IList<object> items) => throw new NotImplementedException();

        internal override void Dispatch(object msg)
        {
            OnDispathch(msg);
            //_next.PostItem(msg);
            OnDispathched(msg);
        }
    }
}
