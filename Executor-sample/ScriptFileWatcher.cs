using System;
using System.IO;
using System.Threading;

namespace Executor.Example
{
    public class ScriptFileWatcher
    {
        Timer _timer;
        ScriptExecuteNode _script_node;
        FileSystemWatcher _watcher;
        public ScriptFileWatcher(string scriptPath, ScriptExecuteNode scriptNode)
        {
            _script_node = scriptNode;
            _watcher = new FileSystemWatcher(scriptPath);
            _watcher.Changed += new FileSystemEventHandler(Watcher_Changed);
            _watcher.Created += new FileSystemEventHandler(Watcher_Changed);
            _watcher.Deleted += new FileSystemEventHandler(Watcher_Changed);
            _watcher.Error += Watcher_Error;
            _watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
        }

        public void StartWatch()
        {
            _timer = new Timer(DoWatcherChanged, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("Script file has changed error:{0}", e.GetException().ToString());
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                _timer.Change(5 * 1000, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Console.WriteLine("watcher_Changed error:{0}", ex);
            }
        }

        private void DoWatcherChanged(object state)
        {
            _script_node.Wait();
            try
            {
                _script_node.UpdateScriptor();
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DoWatcherChanged error:{0}", ex);
            }
            finally
            {
                _script_node.Release();
            }
        }
    }
}
