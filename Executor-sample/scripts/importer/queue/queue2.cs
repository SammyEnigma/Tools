public partial class QueueImportScriptor
{
    public void OnConfigLoad()
    {
        System.Console.WriteLine("执行脚本中的On_configload方法");
    }

    public void OnConfigLoaded()
    {
        System.Console.WriteLine("执行脚本中的On_configloaded方法");
    }
}