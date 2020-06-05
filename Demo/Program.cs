using Demo.Redis;
using System;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            //RedisToolsUseDemo.UseDemo();
            //Pub可以后运行，先运行Sub，运行多个都可以，输入字符串要一样，
            RedisToolsUseDemo.Pub();
            Console.Read();
        }
    }
}
