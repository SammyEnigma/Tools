using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedisTool.Helper
{
    public interface IRedisHelper
    {
        IDatabase DB();
        T HashGet<T>(string key, bool isLock = false) where T : class;
        void HashSet<T>(string key, T model, bool isLock = false) where T : class;
    }
}
