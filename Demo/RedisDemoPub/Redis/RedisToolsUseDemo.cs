using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RedisTools;

namespace Demo.Redis
{
    public class RedisToolsUseDemo
    {
        private static RedisHelper redisHelper = new RedisHelper("localhost:6379,password=123456,abortConnect=false,connectTimeout=10000");
        public static void UseDemo()
        {
            var data = new A
            {
                a = "test",
                b = "test for redis",
                c = 1,
                d = 0.2d,
                f = DateTime.Now,
                g = true
            };
            var aKey = "akey";
            redisHelper.Add(aKey, data);//添加
            var dataB = redisHelper.Get<A>(aKey);//获取
            Console.WriteLine(dataB.a);
            redisHelper.Remove(aKey);//删除
            Console.WriteLine(redisHelper.Exists(aKey));//是否存在
        }

        public static async Task Pub()
        {
            Console.WriteLine("请输入要向那个通道发送消息？");
            var channel = Console.ReadLine();

            await Task.Delay(10);
            for (int i = 0; i < 10; i++)
            {
                await redisHelper.PublishAsync(channel, i.ToString());
            }

        }
    }
    public class A
    {
        public string a;
        public string b;
        public int c;
        public double d;
        public DateTime f;
        public bool g;
    }
}
