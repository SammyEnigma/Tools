namespace Executor.Example
{
    public interface IScriptor
    { }

    public interface IImportScriptor : IScriptor
    {
        void OnConfigLoad();
        void OnConfigLoaded();
        void OnDispatch(object item);
        void OnDispatched(object item);
    }
}
