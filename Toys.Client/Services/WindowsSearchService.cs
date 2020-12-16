using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class WindowsSearchService : ISearchService
    {
        readonly List<FileSystemWatcher> watchers;
        private readonly WindowsSearcher searcher;

        public WindowsSearchService(SearchSetting setting)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
            if (!setting.Enable || setting.SearchPaths.Count == 0) return;

            SearchHistory.Load();
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
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName | NotifyFilters.Attributes,
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

            searcher = new WindowsSearcher(extensions, setting.SearchPaths, setting.MaxCount);
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
        readonly int maxCount = 7;
        readonly WshShell shell = new WshShell();
        readonly Dictionary<string, string> lnk2real = new Dictionary<string, string>();
        readonly Dictionary<string, SearchEntry> refCount = new Dictionary<string, SearchEntry>();

        public WindowsSearcher(List<string> extensions, List<string> folders, int limit)
        {
            maxCount = limit;
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
                    refCount[path].Aliases.Add(alias.ToLower());
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
                    for (int i = 0; i < refCount[path].Aliases.Count; i++)
                    {
                        if (refCount[path].Aliases[i] == alias)
                        {
                            idx = i;
                            break;
                        }
                    }
                    if (idx != -1)
                    {
                        refCount[path].Aliases.RemoveAt(idx);
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
            DirectoryInfo dir = new DirectoryInfo(folder);
            foreach (FileInfo f in dir.GetFiles(extension).Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)))
            {
                allFiles.Add(f.FullName);
            }
            foreach (DirectoryInfo d in dir.GetDirectories().Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)))
            {
                try
                {
                    GetAllFiles(d.FullName, extension, ref allFiles);
                }
                catch
                {
                    Debug.Print("Can not access dir {0}", d.FullName);
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
            // TODO: 有潜在 BUG
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
            else
            {
                FileInfo f = new FileInfo(fullPath);
                if (f.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    DeleteFile(fullPath);
                }
                else if (!lnk2real.ContainsKey(fullPath))
                {
                    AddFile(fullPath);
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
            word = word.ToLower();
            List<SearchEntry> res = new List<SearchEntry>();
            var kvList = new List<(SearchEntry, double)> { };
            foreach (var item in refCount.Values.ToList())
            {
                if (System.IO.File.Exists(item.FullPath))
                {
                    double score = 0;
                    foreach (string t in item.Aliases)
                    {
                        if (t.Contains(word))
                        {
                            score = SearchHistory.Get(item.FullPath);
                            if (score > 0)
                            {
                                break;
                            }
                            score = Math.Max(score, 1.0 * word.Length / t.Length);
                        }
                    }
                    if (score > 0)
                    {
                        if (kvList.Count == 0)
                        {
                            kvList.Add((item, score));
                        }
                        else if (kvList.Count < maxCount)
                        {
                            int idx = kvList.Count - 1;
                            if (kvList[idx].Item2 >= score)
                            {
                                kvList.Add((item, score));
                            }
                            else
                            {
                                kvList.Add(kvList[^1]);
                                idx--;
                                while (idx >= 0 && kvList[idx].Item2 < score)
                                {
                                    kvList[idx + 1] = kvList[idx];
                                    idx--;
                                }
                                kvList[idx + 1] = (item, score);
                            }
                        }
                        else if (kvList[maxCount - 1].Item2 < score)
                        {
                            int idx = maxCount - 2;
                            while (idx >= 0 && kvList[idx].Item2 < score)
                            {
                                kvList[idx + 1] = kvList[idx];
                                idx--;
                            }
                            kvList[idx + 1] = (item, score);
                        }
                    }
                }
            }

            foreach (var i in kvList)
            {
                res.Add(i.Item1);
            }
            return res;
        }
    }

    static class SearchHistory
    {
        static private readonly string historyFilename = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Toys", "history.json");
        public static Dictionary<string, int> records;

        public static void Increase(string fullPath)
        {
            if (records.ContainsKey(fullPath))
            {
                records[fullPath]++;
            }
            else
            {
                records[fullPath] = 1;
            }
            Dump();
        }

        public static int Get(string fullPath)
        {
            if (records.ContainsKey(fullPath))
            {
                return records[fullPath];
            }
            return 0;
        }

        public static void Load()
        {
            if (!System.IO.File.Exists(historyFilename))
            {
                records = new Dictionary<string, int>();
                Dump();
            }

            string json;
            using (FileStream fsRead = new FileStream(historyFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int fsLen = (int)fsRead.Length;
                byte[] heByte = new byte[fsLen];
                fsRead.Read(heByte, 0, heByte.Length);
                json = System.Text.Encoding.UTF8.GetString(heByte);
            }
            records = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
        }

        public static void Dump()
        {
            try
            {
                string json = JsonConvert.SerializeObject(records);
                if (!System.IO.File.Exists(historyFilename))
                {
                    FileInfo file_info = new FileInfo(historyFilename);
                    Directory.CreateDirectory(file_info.DirectoryName);
                }
                System.IO.File.WriteAllText(historyFilename, json);
            }
            catch (Exception)
            {

            }
        }
    }
}
