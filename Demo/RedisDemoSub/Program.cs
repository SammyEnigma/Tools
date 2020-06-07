using System;
using System.Threading.Tasks;
using RedisTools;

namespace RedisDemoSub
{
    class Program
    {
        private static RedisBase redisBase = new RedisBase("localhost:6379,password=123456,abortConnect=false,connectTimeout=10000");
        private static RedisHelper redisHelper = new RedisHelper(redisBase);
        static void Main(string[] args)
        {
            Sub();
            Console.Read();
        }
        public static async Task Sub()
        {
            Console.WriteLine("请输入您要订阅哪个通道的名称？");
            var channelKey = Console.ReadLine();
            await redisHelper.SubscribeAsync<string>(channelKey, PrintData);
            Console.WriteLine("您订阅的通道为：【" + channelKey + "】! 请耐心等待消息！！");
        }
        public static Task PrintData(string data)
        {
            return Task.Run(() => Console.WriteLine(data));
        }
    }
}
