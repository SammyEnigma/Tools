using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedisTools
{
    public interface IRedisConnectionFactory
    {
        IConnectionMultiplexer Get();
    }
}
