using Executor.Example;
using System;

public partial class QueueImportScriptor : IImportScriptor
{
    public void OnDispatch(object item)
    {
        Console.WriteLine("执行脚本中的On_dispatch方法");
    }

    public void OnDispatched(object item)
    {
        Console.WriteLine("执行脚本中的On_dispatched方法");
    }
}