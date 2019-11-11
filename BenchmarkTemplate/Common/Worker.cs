using System;

namespace BenchmarkTemplate
{
    public class Worker : IWorker
    {
        private int _opType;
        private int _itemsCount;
        private ICache _cache;

        public Worker(ICache cache, int opType, int itemsCount)
        {
            _cache = cache;
            _opType = opType;
            _itemsCount = itemsCount;
        }

        public void Start()
        {
            var i = 0;
            var rand = new Random();
            while (i < _itemsCount)
            {
                if (_opType == OpType.AllRead)
                {
                    _cache.Get(i.ToString());
                }
                else if (_opType == OpType.AllWrite)
                {
                    _cache.Set(i.ToString(), i);
                }
                else
                {
                    // 十位代表读操作占比，个位代表写操作占比
                    var left = _opType / 10;
                    if (rand.Next(0, 10) <= left)
                    {
                        _cache.Get(i.ToString());
                    }
                    else
                    {
                        _cache.Set(i.ToString(), i);
                    }
                }
                i++;
            }
        }
    }
}
