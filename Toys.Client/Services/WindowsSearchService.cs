using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

using Toys.Client.Models;

namespace Toys.Client.Services
{
    class WindowsSearchService : ISearchService
    {
        private bool enable;
        private readonly Task t;
        private readonly FileTree tree;
        private static readonly int timerInterval = 5 * 1000;
        private static Timer _timer;

        public WindowsSearchService(SearchSetting setting)
        {
            if (!setting.Enable || setting.SearchPaths.Count == 0)
            {
                enable = false;
                return;
            }
            enable = true;

            SearchHistory.Load();

            tree = new FileTree(setting);
            _timer = new Timer(timerInterval)
            {
                AutoReset = true,
                Enabled = true,
            };

            _timer.Elapsed += Update;
            t = Task.Run(() => Update(null, null));
        }

        // 不支持 隐藏 文件索引实时更新
        private void Update(object source, ElapsedEventArgs e)
        {
#if DEBUG
            //System.Threading.Thread.Sleep(10000);
            //if (e is null)
            //{
            //    Debug.Print("Create index at {0:HH:mm:ss.fff}", DateTime.Now);
            //}
            //else
            //{
            //    Debug.Print("Update index at {0:HH:mm:ss.fff}", e.SignalTime);
            //}

            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            _timer.Stop();
            tree.Rescan();
            _timer.Start();
#if DEBUG
            stopwatch.Stop();
            string elapsed_time = stopwatch.ElapsedMilliseconds.ToString();
            Debug.Print("Time elapse " + elapsed_time);
#endif
        }

        public void Reload(SearchSetting setting)
        {
            // TODO immediately reload
            _timer.Stop();
            if (!setting.Enable || setting.SearchPaths.Count == 0)
            {
                enable = false;
                return;
            }
            enable = true;
            Task.Run(() => tree.Reload(setting));
            _timer.Start();
        }

        public List<SearchEntry> Search(string word)
        {
            if (!enable)
            {
                return new List<SearchEntry>();
            }

            if (!t.IsCompleted)
            {
                return new List<SearchEntry>() { new SearchEntry { Display = "搜索服务创建索引中..." } };
            }

            word = word.ToLower();
            return tree.Search(word);
        }
    }

    class FileTree
    {
        class PathNode
        {
            class Index
            {
                readonly Dictionary<string, List<PathNode>> map = new Dictionary<string, List<PathNode>>();
                public void Add(PathNode node)
                {
                    string key = FileHelper.FileName(node.Path).ToLower();
                    if (!map.ContainsKey(key))
                    {
                        map.Add(key, new List<PathNode>() { node });
                    }
                    else
                    {
                        map[key].Add(node);
                    }
                }

                public void Remove(PathNode node)
                {
                    if (node is null)
                    {
                        return;
                    }

                    if (node.Children != null)
                    {
                        foreach (var child in node.Children)
                        {
                            Remove(child);
                        }
                    }
                    string key = FileHelper.FileName(node.Path).ToLower();
                    for (int i = map[key].Count - 1; i >= 0; i--)
                    {
                        if (map[key][i].Path == node.Path)
                        {
                            map[key].RemoveAt(i);
                        }
                    }
                }

                public List<SearchEntry> Search(string word, int maxCount)
                {
                    double length = 1.0 * word.Length;
                    var kvList = new List<(string, double)> { };
                    foreach (string name in map.Keys)
                    {
                        if (name.Contains(word))
                        {
                            double baseScore = length / name.Length;
                            foreach (PathNode node in map[name])
                            {
                                string path = node.Path;
                                double score = baseScore + SearchHistory.Get(path);
                                if (File.Exists(path))
                                {
                                    score += 0.8;
                                }

                                if (kvList.Count == 0)
                                {
                                    kvList.Add((path, score));
                                }
                                else if (kvList.Count < maxCount)
                                {
                                    int idx = kvList.Count - 1;
                                    if (kvList[idx].Item2 >= score)
                                    {
                                        kvList.Add((path, score));
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
                                        kvList[idx + 1] = (path, score);
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
                                    kvList[idx + 1] = (path, score);
                                }
                            }
                        }
                    }

                    List<SearchEntry> res = new List<SearchEntry>();
                    foreach (var i in kvList)
                    {
                        res.Add(new SearchEntry(i.Item1, FileHelper.FileName(i.Item1)));
                    }
                    return res;
                }
            }

            private static readonly Index index = new Index();

            public string Path;
            public DateTime LastWriteTime;
            private List<PathNode> Children;

            public PathNode(string fullPath)
            {
                Path = fullPath;
                if (Path != "")
                {
                    Debug.Print("Add: {0}", Path);
                    LastWriteTime = File.GetLastWriteTime(fullPath);
                    index.Add(this);
                }
            }

            public void AddChild(string path)
            {
                AddChild(new PathNode(path));
            }

            public void AddChild(PathNode node)
            {
                if (node is null)
                {
                    return;
                }
                if (Children is null)
                {
                    Children = new List<PathNode>();
                }
                Children.Add(node);
            }

            public bool TryAddChild(string path)
            {
                if (Children is null)
                {
                    AddChild(path);
                    return true;
                }
                for (int i = 0; i < Children.Count; i++)
                {
                    if (Children[i].Path == path)
                    {
                        return false;
                    }
                }
                AddChild(path);
                return true;
            }

            public int GetChildIdx(string path)
            {
                if (Children is null)
                {
                    return -1;
                }
                for (int i = 0; i < Children.Count; i++)
                {
                    if (Children[i].Path == path)
                    {
                        return i;
                    }
                }
                return -1;
            }

            public PathNode GetChild(int idx)
            {
                return Children[idx];
            }

            public List<PathNode> ListChildren()
            {
                if (Children is null)
                {
                    return new List<PathNode>();
                }
                return Children;
            }

            public void Prune(List<string> keep)
            {
                for (int i = Children.Count - 1; i >= 0; i--)
                {
                    if (!keep.Contains(Children[i].Path))
                    {
                        index.Remove(Children[i]);
                        Children.RemoveAt(i);
                    }
                }
            }

            public void ReplaceChild(int idx, PathNode node)
            {
                index.Remove(Children[idx]);
                Children[idx] = node;
            }

            public static List<SearchEntry> Search(string word, int maxCount)
            {
                return index.Search(word, maxCount);
            }

            ~PathNode()
            {
                Debug.Print("Destroy: {0}", Path);
            }
        }

        List<string> Extensions;
        int MaxCount;
        List<SearchSetting.SeachPath> SearchPaths;
        readonly Dictionary<string, int> PathOldDepth = new Dictionary<string, int>();

        readonly PathNode root = new PathNode("");

        public FileTree(SearchSetting setting)
        {
            Reload(setting);
        }

        public void Reload(SearchSetting setting)
        {
            Extensions = setting.Extensions;
            for (int i = 0; i < Extensions.Count; i++)
            {
                Extensions[i] = Extensions[i].ToLower();
            }

            MaxCount = setting.MaxCount;

            SearchPaths = new List<SearchSetting.SeachPath>();
            foreach (var item in setting.SearchPaths.OrderByDescending(i => i.Path).ToList())
            {
                if (SearchPaths.Count > 0 && SearchPaths[^1].Path == item.Path)
                {
                    SearchPaths[^1] = item;
                }
                else
                {
                    SearchPaths.Add(item);
                }
            }
            Rescan();
        }

        public void Rescan()
        {
            List<string> keep = new List<string>();
            foreach (SearchSetting.SeachPath item in SearchPaths)
            {
                // 确定是文件夹
                string path = item.Path;
                int depth = item.MaxDepth;

                if (!PathOldDepth.ContainsKey(path))
                {
                    root.AddChild(Create(path, depth));
                }
                else
                {
                    int idx = root.GetChildIdx(path);
                    if (PathOldDepth[path] == depth)
                    {
                        Update(root.GetChild(idx), depth);
                    }
                    else
                    {
                        root.ReplaceChild(idx, Create(path, depth, true));
                    }
                }
                PathOldDepth[path] = depth;
                keep.Add(path);
            }

            root.Prune(keep);
            List<string> drop = new List<string>();
            foreach (string key in PathOldDepth.Keys)
            {
                if (!keep.Contains(key)) { drop.Add(key); }
            }
            foreach (string key in drop)
            {
                PathOldDepth.Remove(key);
            }
        }

        private PathNode Create(string path, int depth, bool forRoot = false)
        {
            if (depth < 0 || (!forRoot && root.GetChildIdx(path) != -1))
            {
                return null;
            }

            PathNode node = new PathNode(path);
            foreach (string file in FileHelper.ListFile(path, Extensions))
            {
                node.AddChild(file);
            }
            foreach (string folder in FileHelper.ListDirectory(path))
            {
                node.AddChild(Create(folder, depth - 1));
            }
            return node;
        }

        private void Update(PathNode node, int depth)
        {
            if (depth < 1) return;

            if (node.LastWriteTime < Directory.GetLastWriteTime(node.Path))
            {
                List<string> keep = new List<string>();
                foreach (string file in FileHelper.ListFile(node.Path, Extensions))
                {
                    keep.Add(file);
                    node.TryAddChild(file);
                }

                foreach (string folder in FileHelper.ListDirectory(node.Path))
                {
                    keep.Add(folder);
                    int idx = node.GetChildIdx(folder);
                    if (idx != -1)
                    {
                        Update(node.GetChild(idx), depth - 1);
                    }
                    else
                    {
                        node.AddChild(Create(folder, depth - 1));
                    }
                }
                node.Prune(keep);
                node.LastWriteTime = Directory.GetLastWriteTime(node.Path);
            }
            else
            {
                List<string> folders = FileHelper.ListDirectory(node.Path);
                foreach (PathNode child in node.ListChildren())
                {
                    if (folders.Contains(child.Path))
                    {
                        Update(child, depth - 1);
                    }
                }
            }
        }

        public List<SearchEntry> Search(string word)
        {
            return PathNode.Search(word, MaxCount);
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
            if (!File.Exists(historyFilename))
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
                if (!File.Exists(historyFilename))
                {
                    FileInfo file_info = new FileInfo(historyFilename);
                    Directory.CreateDirectory(file_info.DirectoryName);
                }
                File.WriteAllText(historyFilename, json);
            }
            catch (Exception)
            {
                Debug.Print("Dump failed");
            }
        }
    }

    static class FileHelper
    {
        static readonly HashSet<string> deniedPath = new HashSet<string>();
        static readonly List<string> stopWords = new List<string>()
        {
            "修复", "卸载", "删除", "安装", "install", "uninstall"
        };
        static readonly List<string> stopTargets = new List<string>()
        {
            "cmd.exe", "pythonw.exe", "pwsh.exe", "powershell.exe", "python.exe",
            "explorer.exe", "java.exe", "javacpl.exe", "javaw.exe", "javaws.exe",
        };

        public static bool IsDirectory(string path)
        {
            return Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.Directory);
        }

        public static bool IsFile(string path)
        {
            return Exists(path) && !File.GetAttributes(path).HasFlag(FileAttributes.Directory);
        }

        public static string FileName(string path)
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            if (filename == "")
            {
                filename = Path.GetFileName(path);  // 特殊文件如 .bashrc
            }
            return filename;
        }

        public static bool Exists(string path)
        {
            return Directory.Exists(path) || File.Exists(path);
        }

        public static List<string> ListFile(string path, List<string> exts)
        {
            List<string> res = new List<string>();
            if (Directory.Exists(path))
            {
                foreach (string f in Directory.GetFiles(path))
                {
                    if (exts.Count == 0 || Path.GetFileNameWithoutExtension(f) == "" || (new FileInfo(f).Extension != "" && exts.Contains(new FileInfo(f).Extension[1..])))
                    {
                        bool valid = true;
                        foreach (string word in stopWords)
                        {
                            if (f.ToLower().Contains(word))
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (valid) res.Add(f);
                    }
                }
            }
            return res;
        }

        public static List<string> ListDirectory(string path)
        {
            List<string> res = new List<string>();

            if (Directory.Exists(path))
            {
                foreach (string d in Directory.GetDirectories(path))
                {
                    if (deniedPath.Contains(d)) continue;
                    try
                    {
                        Directory.GetDirectories(d);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Debug.Print("Access to {0} deny", d);
                        deniedPath.Add(d);
                        continue;
                    }
                    res.Add(d);
                }
            }
            return res;
        }

        public static bool IsHidden(string path)
        {
            if (IsFile(path))
            {
                return new FileInfo(path).Attributes.HasFlag(FileAttributes.Hidden);
            }
            else
            {
                return new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.Hidden);
            }
        }
    }
}
