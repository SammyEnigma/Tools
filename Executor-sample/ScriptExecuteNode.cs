using CSScriptLib;
using System.IO;
using System.Text;
using System.Threading;

namespace Executor.Example
{
    public abstract class ScriptExecuteNode : ExecuteNode
    {
        private SemaphoreSlim _semaphore;
        protected IImportScriptor _scriptor;
        protected ScriptFileWatcher _watcher;
        protected string _script_path;

        public ScriptExecuteNode(bool concurrency, string scriptPath)
            : base(concurrency)
        {
            _script_path = Path.Combine("scripts", scriptPath);
            _semaphore = new SemaphoreSlim(1, 1);
            LoadScriptor();
            StartWatcher();
        }

        internal void StartWatcher()
        {
            _watcher = new ScriptFileWatcher(_script_path, this);
            _watcher.StartWatch();
        }

        internal virtual void LoadScriptor()
        {
            var sb = new StringBuilder();
            foreach (var file in Directory.GetFiles(_script_path))
            {
                var content = File.ReadAllText(file);
                sb.AppendLine(content);
            }
            _scriptor = (IImportScriptor)CSScript.Evaluator.LoadCode(sb.ToString());
        }

        internal virtual void UpdateScriptor()
        {
            LoadScriptor();
        }

        internal void Wait()
        {
            _semaphore.Wait();
        }

        internal void Release()
        {
            _semaphore.Release();
        }
    }
}
