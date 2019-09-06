using System.IO;

namespace Executor.Example
{
    public abstract class Importer : ScriptExecuteNode
    {
        protected string _config;
        protected ExecuteNode _next;

        public abstract void LoadConfig();
        internal abstract void Dispatch(object msg);

        public Importer(bool concurrency, string scriptPath)
            : base(concurrency, Path.Combine("importer", scriptPath))
        {
            LoadConfig();
        }

        protected void OnConfigLoad()
        {
            Wait();
            try
            {
                _scriptor.OnConfigLoad();
            }
            finally
            {
                Release();
            }
        }

        protected void OnConfigLoaded()
        {
            Wait();
            try
            {
                _scriptor.OnConfigLoaded();
            }
            finally
            {
                Release();
            }
        }

        protected void OnDispathch(object item)
        {
            Wait();
            try
            {
                _scriptor.OnDispatch(item);
            }
            finally
            {
                Release();
            }
        }

        protected void OnDispathched(object item)
        {
            Wait();
            try
            {
                _scriptor.OnDispatched(item);
            }
            finally
            {
                Release();
            }
        }
    }
}
