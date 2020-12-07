using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class SearchService : ISearchService
    {
        readonly List<FileSystemWatcher> watchers;
        private readonly WindowsSearcher searcher;

        public SearchService(SearchSetting setting)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
            if (!setting.Enable || setting.SearchPaths.Count == 0) return;

            List<string> extensions = new List<string>();
            if (!setting.Extensions.Contains("*") && setting.Extensions.Count != 0)
            {
                foreach (string s in setting.Extensions)
                {
                    extensions.Add("*." + s.ToLower());
                }
            }
            else
            {
                extensions.Add("*.*");
            }

            watchers = new List<FileSystemWatcher>();
            foreach (string fn in setting.SearchPaths)
            {
                var watcher = new FileSystemWatcher()
                {
                    Path = fn,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = true
                };
                foreach (string filter in extensions)
                {
                    watcher.Filters.Add(filter);
                }

                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.Created += new FileSystemEventHandler(OnCreated);
                watcher.Deleted += new FileSystemEventHandler(OnDeleted);
                watcher.Renamed += new RenamedEventHandler(OnRenamed);
                watchers.Add(watcher);
            }

            searcher = new WindowsSearcher(extensions, setting.SearchPaths);
        }

        public List<SearchEntry> Search(string keyword)
        {
            return searcher.Search(keyword);
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            searcher.ChangeFile(e.FullPath);
        }

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            searcher.AddFile(e.FullPath);
        }

        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            searcher.DeleteFile(e.FullPath);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            searcher.RenameFile(e.OldFullPath, e.FullPath);
        }
    }

    class WindowsSearcher
    {
        readonly WshShell shell = new WshShell();
        readonly Dictionary<string, string> lnk2real = new Dictionary<string, string>();
        readonly Dictionary<string, SearchEntry> refCount = new Dictionary<string, SearchEntry>();

        public WindowsSearcher(List<string> extensions, List<string> folders)
        {
            Task.Run(() => Init(ref extensions, ref folders));
        }

        private void Increase(string path, string alias = null)
        {
            path = path.Replace("/", "\\");
            if (refCount.ContainsKey(path))
            {
                refCount[path].Count += 1;
                if (alias != null)
                {
                    refCount[path].Matches.Add(alias);
                }
            }
            else
            {
                refCount[path] = new SearchEntry(path, alias);
            }
        }

        private void Decrease(string path, string alias = null)
        {
            path = path.Replace("/", "\\");
            if (refCount.ContainsKey(path))
            {
                refCount[path].Count -= 1;
                if (refCount[path].Count == 0)
                {
                    refCount.Remove(path);
                }
                else if (alias != null)
                {
                    int idx = -1;
                    for (int i = 0; i < refCount[path].Matches.Count; i++)
                    {
                        if (refCount[path].Matches[i] == alias)
                        {
                            idx = i;
                            break;
                        }
                    }
                    if (idx != -1)
                    {
                        refCount[path].Matches.RemoveAt(idx);
                    }
                }
            }
        }

        private void Remove(string path)
        {
            if (refCount.ContainsKey(path))
            {
                refCount.Remove(path);
            }
        }

        private void GetAllFiles(string folder, string extension, ref List<string> allFiles)
        {
            foreach (string file in Directory.GetFiles(folder, extension))
            {
                allFiles.Add(file);
            }
            foreach (string subDir in Directory.GetDirectories(folder))
            {
                try
                {
                    GetAllFiles(subDir, extension, ref allFiles);
                }
                catch
                {
                    Debug.Print("Can not access dir {0}", subDir);
                }
            }
        }

        private void Init(ref List<string> extensions, ref List<string> folders)
        {
            List<string> allFiles = new List<string>();
            foreach (string folder in folders)
            {
                foreach (string extension in extensions)
                {
                    GetAllFiles(folder, extension, ref allFiles);
                }
            }

            List<string> support = new List<string>();
            foreach (string extension in extensions)
            {
                support.Add(extension[1..]);
            }

            foreach (string file in allFiles)
            {
                FileInfo info = new FileInfo(file);
                string ext = info.Extension;
                string fn = info.Name.Substring(0, info.Name.Length - ext.Length);
                if (!info.Exists || fn.Contains("卸载") || fn.Contains("Uninstall") || fn.Contains("删除")) continue;

                string path = info.FullName;
                if (ext == ".lnk")
                {
                    path = ((IWshShortcut)shell.CreateShortcut(path)).TargetPath;
                    if (!System.IO.File.Exists(path) || !support.Contains(Path.GetExtension(path))) continue;
                }
                Increase(path, fn);
            }
        }

        public void ChangeFile(string fullPath)
        {
            fullPath = fullPath.Replace("/", "\\");
            Debug.Print("change file {0}", fullPath);
            if (fullPath.EndsWith(".lnk"))
            {
                string alias = Path.GetFileNameWithoutExtension(fullPath);
                if (lnk2real.ContainsKey(fullPath))
                {
                    string oldTarget = lnk2real[fullPath];
                    Decrease(oldTarget, alias);
                }

                string newTarget = ((IWshShortcut)shell.CreateShortcut(fullPath)).TargetPath;
                if (System.IO.File.Exists(newTarget))
                {
                    lnk2real[fullPath] = newTarget;
                    Increase(newTarget, alias);
                }
            }
        }

        public void AddFile(string fullPath)
        {
            fullPath = fullPath.Replace("/", "\\");
            Debug.Print("add file {0}", fullPath);
            if (fullPath.EndsWith(".lnk"))
            {
                string alias = Path.GetFileNameWithoutExtension(fullPath);
                string target = ((IWshShortcut)shell.CreateShortcut(fullPath)).TargetPath;
                if (System.IO.File.Exists(target))
                {
                    lnk2real[fullPath] = target;
                    Increase(target, alias);
                }
            }
            else
            {
                if (System.IO.File.Exists(fullPath))
                {
                    Increase(fullPath);
                }
            }
        }

        public void DeleteFile(string fullPath)
        {
            fullPath = fullPath.Replace("/", "\\");
            Debug.Print("delete file {0}", fullPath);
            if (fullPath.EndsWith(".lnk"))
            {
                if (lnk2real.ContainsKey(fullPath))
                {
                    string alias = Path.GetFileNameWithoutExtension(fullPath);
                    string oldTarget = lnk2real[fullPath];
                    lnk2real.Remove(fullPath);
                    Decrease(oldTarget, alias);
                }
            }
            else
            {
                Remove(fullPath);
            }
        }

        public void RenameFile(string oldFullPath, string newFullPath)
        {
            oldFullPath = oldFullPath.Replace("/", "\\");
            newFullPath = newFullPath.Replace("/", "\\");
            Debug.Print("rename file {0} -> {1}", oldFullPath, newFullPath);
            if (oldFullPath.EndsWith(".lnk"))
            {
                string oldAlias = Path.GetFileNameWithoutExtension(oldFullPath);
                string newAlias = Path.GetFileNameWithoutExtension(newFullPath);
                string target = lnk2real[oldFullPath];
                lnk2real.Remove(oldFullPath);
                lnk2real[newFullPath] = target;
                Decrease(target, oldAlias);
                Increase(target, newAlias);
            }
            else
            {
                Remove(oldFullPath);
                Increase(newFullPath);
            }
        }

        public List<SearchEntry> Search(string word)
        {
            string[] words = word.ToLower().Split(' ');
            List<SearchEntry> res = new List<SearchEntry>();
            foreach (var item in refCount)
            {
                if (item.Value.Match(words) && System.IO.File.Exists(item.Value.FullPath))
                {
                    res.Add(item.Value);
                }
            }
            return res;
        }
    }
}
