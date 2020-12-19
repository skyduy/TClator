using System;
using System.IO;
using System.Linq;
using System.Timers;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;
using IWshRuntimeLibrary;

using Toys.Client.Models;

namespace Toys.Client.Services
{
    class WindowsSearchService : ISearchService
    {
        private bool enable;
        private readonly Task t;
        private readonly FileForest index;
        private static readonly int timerInterval = 3 * 1000;
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

            index = new FileForest(setting);
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
            if (e is null)
            {
                Debug.Print("Create index at {0:HH:mm:ss.fff}", DateTime.Now);
            }
            else
            {
                Debug.Print("Update index at {0:HH:mm:ss.fff}", e.SignalTime);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            _timer.Stop();
            index.Rescan();
            _timer.Start();
#if DEBUG
            stopwatch.Stop();
            string elapsed_time = stopwatch.ElapsedMilliseconds.ToString();
            Debug.Print("Time elapse " + elapsed_time);
#endif
        }

        public void Reload(SearchSetting setting)
        {
            _timer.Stop();
            if (!setting.Enable || setting.SearchPaths.Count == 0)
            {
                enable = false;
                return;
            }
            enable = true;
            Task.Run(() => index.Reload(setting));
            _timer.Start();
        }

        public List<SearchEntry> Search(string word)
        {
            List<SearchEntry> res = new List<SearchEntry>();
            if (!enable)
            {
                return res;
            }

            if (!t.IsCompleted)
            {
                res.Add(new SearchEntry { Display = "搜索服务创建索引中..." });
                return res;
            }

            word = word.ToLower();
            foreach (var node in index.Search(word))
            {
                res.Add(new SearchEntry(node.FullPath, node.Display));
            }
            return res;
        }

    }

    enum FileType
    {
        File,
        Directory,
        Link,
    }

    class PathNode
    {
        public string FileName;
        public string FullPath;
        public FileType Type;
        public DateTime LastWriteTime;
        public int RemainDepth;
        public string Display;
        public PathNode Parent;
        public Dictionary<string, PathNode> Children;

        private List<string> Nicknames;
        private List<string> Aliases;

        public PathNode(string fullPath)
        {
            FullPath = fullPath;
            Display = FileHelper.FileName(fullPath);
            Nicknames = new List<string>() { Display.ToLower() };
            Debug.Print("Created: {0}", FullPath);
        }

        public void AddLnk(string path)
        {
            if (Aliases == null)
            {
                Aliases = new List<string>() { path };
                Display = FileHelper.FileName(path);
                Nicknames.Add(Display.ToLower());
            }
            else
            {
                Aliases.Add(path);
                Display = FileHelper.FileName(FullPath);
                Nicknames.Add(FileHelper.FileName(path).ToLower());
            }
        }

        public void RemoveLnk(string path)
        {
            Aliases.Remove(path);
            Nicknames.Remove(FileHelper.FileName(path).ToLower());

            if (Aliases.Count == 0)
            {
                Display = FileHelper.FileName(FullPath);
            }
            else if (Aliases.Count == 1)
            {
                Display = FileHelper.FileName(Aliases[0]);
            }
        }

        public void PruneLnk()
        {
            List<string> rms = new List<string>();
            foreach (string path in Aliases)
            {
                if (FileHelper.LnkFile(path) != FullPath)
                {
                    rms.Add(path);
                }
            }
            foreach (string path in rms)
            {
                RemoveLnk(path);
            }
        }

        public bool IsAlone()
        {
            return Aliases == null || Aliases.Count == 0;
        }

        public double MatchScore(string word)
        {
            double score = 0;
            foreach (string t in Nicknames)
            {
                if (t.Contains(word))
                {
                    score = Math.Max(score, 1.0 * word.Length / t.Length);
                }
            }

            return score;
        }

        ~PathNode()
        {
            Debug.Print("Destroy: {0}", FullPath);
        }
    }

    class FileForest
    {
        List<string> Extensions;
        int MaxCount;
        bool PrivacyMode;
        List<SearchSetting.SeachPath> SearchPaths;
        readonly Dictionary<string, PathNode> roots = new Dictionary<string, PathNode>();
        readonly Dictionary<string, PathNode> island = new Dictionary<string, PathNode>();

        readonly HashSet<PathNode> linked = new HashSet<PathNode>();

        public FileForest(SearchSetting setting)
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
            PrivacyMode = setting.PrivacyMode;

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
        }

        public void Rescan()
        {
            linked.Clear();

            List<string> exists = new List<string>();
            List<string> dropKeys = new List<string>();
            foreach (SearchSetting.SeachPath item in SearchPaths)
            {
                // 确定是文件夹
                string path = item.Path;
                if (FileHelper.LnkFile(item.Path) is not null)
                {
                    if (!Directory.Exists(path)) continue;
                    path = FileHelper.LnkFile(item.Path);
                }

                exists.Add(path);
                if (roots.ContainsKey(path))
                {
                    Update(roots[path], item.MaxDepth);
                }
                else
                {
                    PathNode root = Create(path, item.MaxDepth);
                    root.FileName = root.FullPath;
                    roots[path] = root;
                }
            }

            foreach (string key in roots.Keys)
            {
                if (!exists.Contains(key))
                {
                    dropKeys.Add(key);
                }
            }
            foreach (string key in dropKeys)
            {
                roots.Remove(key);
            }

            foreach (PathNode node in linked)
            {
                node.PruneLnk();
            }
            linked.Clear();
        }

        private PathNode Create(string path, int remainDepth)
        {
            // 递归检索当前目录
            if (FileHelper.IsFile(path))
            {
                if (FileHelper.LnkFile(path) is not null)
                {
                    string targetPath = FileHelper.LnkFile(path);
                    PathNode targetNode = Find(targetPath);
                    if (targetNode == null)
                    {
                        if (!island.ContainsKey(targetPath))
                        {
                            island[targetPath] = Create(targetPath, remainDepth);
                        }
                        targetNode = island[targetPath];
                    }
                    targetNode.AddLnk(path);

                    linked.Add(targetNode);
                    return new PathNode(path)
                    {
                        FileName = Path.GetFileName(path),
                        Type = FileType.Link,
                        LastWriteTime = System.IO.File.GetLastWriteTime(path),
                        Children = new Dictionary<string, PathNode> { { "target", targetNode } },
                    };
                }
                else
                {
                    return new PathNode(path)
                    {
                        FileName = Path.GetFileName(path),
                        Type = FileType.File,
                    };
                }
            }
            else
            {
                PathNode node = new PathNode(path)
                {
                    FileName = Path.GetFileName(path),
                    Type = FileType.Directory,
                    LastWriteTime = Directory.GetLastWriteTime(path),
                    RemainDepth = remainDepth,
                    Children = new Dictionary<string, PathNode>(),
                };

                // 仍可继续递归扫描文件夹
                if (node.RemainDepth > 0)
                {
                    foreach (string fn in FileHelper.ListAll(path, Extensions))
                    {
                        if (island.ContainsKey(fn))  // 孤岛不再孤单
                        {
                            PathNode child = island[fn];
                            island.Remove(fn);
                            child.Parent = node;
                            node.Children[fn] = child;
                        }
                        else
                        {
                            if (FileHelper.IsFile(fn))
                            {
                                PathNode child = Create(fn, node.RemainDepth - 1);
                                child.Parent = node;
                                node.Children[fn] = child;
                            }
                            else
                            {   // 文件夹特殊对待，因为可能被写到配置目录中
                                PathNode child = Find(fn);
                                if (child == null)
                                {
                                    child = Create(fn, node.RemainDepth - 1);
                                }
                                child.Parent = node;
                                node.Children[fn] = child;
                            }
                        }
                    }
                }
                return node;
            }
        }

        private void Delete(PathNode item)
        {
            switch (item.Type)
            {
                case FileType.File:
                case FileType.Directory:
                    if (island.ContainsKey(item.FullPath))
                    {
                        island.Remove(item.FullPath);
                    }
                    else
                    {
                        if (item.Parent != null)
                        {
                            item.Parent.Children.Remove(item.FullPath);
                        }
                    }
                    break;
                case FileType.Link:
                    item.Parent.Children.Remove(item.FullPath);
                    PathNode targetNode = item.Children["target"];
                    targetNode.RemoveLnk(item.FullPath);

                    if (targetNode.IsAlone() && island.ContainsKey(targetNode.FullPath))
                    {
                        island.Remove(targetNode.FullPath);
                    }
                    break;
                default:
                    break;
            }
        }

        private void Update(PathNode item, int remainDepth)
        {
            if (item.Type != FileType.Directory) return;

            if (remainDepth == item.RemainDepth || roots.ContainsKey(item.FullPath))
            {
                if (item.RemainDepth < 1)
                {
                    return;
                }
                else
                {
                    if (item.LastWriteTime < Directory.GetLastWriteTime(item.FullPath))
                    {
                        List<PathNode> dropNodes = new List<PathNode>();
                        List<string> exists = FileHelper.ListAll(item.FullPath, Extensions);

                        foreach (string path in exists)
                        {
                            if (item.Children.ContainsKey(path))
                            {   // 更新目录的子项
                                if (item.Children[path].Type == FileType.Directory)
                                {
                                    Update(item.Children[path], item.RemainDepth - 1);
                                }
                                else if (item.Children[path].Type == FileType.Link && item.Children[path].LastWriteTime < System.IO.File.GetLastWriteTime(path))
                                {
                                    PathNode oldNode = item.Children[path].Children["target"];
                                    oldNode.RemoveLnk(path);
                                    if (oldNode.IsAlone() && island.ContainsKey(oldNode.FullPath))
                                    {
                                        island.Remove(oldNode.FullPath);
                                    }

                                    string target = FileHelper.LnkFile(path);
                                    PathNode newNode = Find(target);
                                    if (newNode != null)
                                    {
                                        newNode.AddLnk(path);
                                    }
                                    else
                                    {
                                        if (!island.ContainsKey(target))
                                        {
                                            island[target] = Create(target, item.RemainDepth - 1);
                                        }
                                        island[target].AddLnk(path);
                                    }
                                    linked.Add(newNode);
                                    item.Children[path].Children["target"] = newNode;
                                }
                            }
                            else
                            {   // 添加目录子项
                                if (item.RemainDepth > 0)
                                {
                                    PathNode child = Create(path, item.RemainDepth - 1);
                                    item.Children[path] = child;
                                    child.Parent = item;
                                }
                            }
                        }

                        // 删除目录子项
                        foreach (PathNode child in item.Children.Values)
                        {
                            if (!exists.Contains(child.FullPath))
                            {
                                dropNodes.Add(child);
                            }
                        }
                        foreach (PathNode node in dropNodes)
                        {
                            Delete(node);
                        }

                        item.LastWriteTime = Directory.GetLastWriteTime(item.FullPath);
                    }
                    else
                    {
                        foreach (string dir in FileHelper.ListDirectory(item.FullPath))
                        {
                            Update(item.Children[dir], item.RemainDepth - 1);
                        }
                    }
                }
            }
            else
            {
                int oldDepth = item.RemainDepth;
                item.RemainDepth = remainDepth;
                if (remainDepth < oldDepth)
                {
                    // 深度缩小
                    if (remainDepth == 0)
                    {
                        foreach (PathNode node in item.Children.Values)
                        {
                            Delete(node);
                        }
                    }
                    else
                    {
                        foreach (string dir in FileHelper.ListDirectory(item.FullPath))
                        {
                            Update(item.Children[dir], item.RemainDepth - 1);
                        }
                    }
                }
                else
                {
                    // 深度增加
                    if (item.RemainDepth == 0)
                    {
                        foreach (string fn in FileHelper.ListAll(item.FullPath, Extensions))
                        {
                            if (island.ContainsKey(fn))  // 孤岛不再孤单
                            {
                                PathNode child = island[fn];
                                island.Remove(fn);
                                child.Parent = item;
                                item.Children[fn] = child;
                            }
                            else
                            {
                                if (FileHelper.IsFile(fn))
                                {
                                    PathNode child = Create(fn, item.RemainDepth - 1);
                                    child.Parent = item;
                                    item.Children[fn] = child;
                                }
                                else
                                {   // 文件夹特殊对待，因为可能被写到配置目录中
                                    PathNode child = Find(fn);
                                    if (child == null)
                                    {
                                        child = Create(fn, item.RemainDepth - 1);
                                    }
                                    child.Parent = item;
                                    item.Children[fn] = child;
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (string dir in FileHelper.ListDirectory(item.FullPath))
                        {
                            Update(item.Children[dir], item.RemainDepth - 1);
                        }
                    }
                }
            }
        }

        public PathNode Find(string path)
        {
            static PathNode _Find(PathNode node, string path)
            {
                if (!path.StartsWith(node.FileName)) return null;

                if (path == node.FileName) return node;

                path = path[(node.FileName.Length + 1)..];
                if (path == "") return node;

                if (node.Children == null) return null;

                PathNode res;
                foreach (PathNode child in node.Children.Values)
                {
                    res = _Find(child, path);
                    if (res != null)
                    {
                        return res;
                    }
                }
                return null;
            }

            PathNode res = null;
            foreach (PathNode node in roots.Values)
            {
                res = _Find(node, path);
                if (res != null)
                {
                    break;
                }
            }
            return res;
        }

        public List<PathNode> Search(string word)
        {
            List<PathNode> res = new List<PathNode>();
            var kvList = new List<(PathNode, double)> { };
            Queue<PathNode> q = new Queue<PathNode>();

            void _check(PathNode item, bool exactMatch = false)
            {
                double score;
                if (exactMatch)
                {
                    if (FileHelper.FileName(item.FullPath).ToLower() == word)
                    {
                        score = 1;
                    }
                    else
                    {
                        score = 0;
                    }
                }
                else
                {
                    score = item.MatchScore(word);
                }

                if (score > 0)
                {
                    score = Math.Max(score, SearchHistory.Get(item.FullPath));

                    // 文件权重更高
                    if (System.IO.File.Exists(item.FullPath))
                    {
                        score += 0.8;
                    }

                    if (kvList.Count == 0)
                    {
                        kvList.Add((item, score));
                    }
                    else if (kvList.Count < MaxCount)
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
                    else if (kvList[MaxCount - 1].Item2 < score)
                    {
                        int idx = MaxCount - 2;
                        while (idx >= 0 && kvList[idx].Item2 < score)
                        {
                            kvList[idx + 1] = kvList[idx];
                            idx--;
                        }
                        kvList[idx + 1] = (item, score);
                    }
                }
            }

            void _enqueue(PathNode item)
            {
                if (item == null || !FileHelper.Exists(item.FullPath) || item.Type == FileType.Link) return;

                if (item.FullPath.Contains("conda"))
                {

                }

                if (!PrivacyMode || !FileHelper.IsHidden(item.FullPath))
                {
                    q.Enqueue(item);
                }
                else
                {
                    _check(item, true);
                }
            }

            foreach (PathNode item in island.Values)
            {
                _enqueue(item);
            }

            foreach (PathNode item in roots.Values)
            {
                _enqueue(item);
            }

            while (q.Count > 0)
            {
                for (int i = 0; i < q.Count; i++)
                {
                    PathNode item = q.Dequeue();
                    _check(item);

                    if (item.Children != null)
                    {
                        foreach (PathNode child in item.Children.Values)
                        {
                            if (!roots.ContainsKey(child.FullPath))
                            {
                                _enqueue(item: child);
                            }
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
            "explorer.exe",
        };
        static readonly WshShell shell = new WshShell();

        public static bool IsDirectory(string path)
        {
            return Exists(path) && System.IO.File.GetAttributes(path).HasFlag(FileAttributes.Directory);
        }

        public static bool IsFile(string path)
        {
            return Exists(path) && !System.IO.File.GetAttributes(path).HasFlag(FileAttributes.Directory);
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
            return Directory.Exists(path) || System.IO.File.Exists(path);
        }

        public static string LnkFile(string lnkPath)
        {
            if (new FileInfo(lnkPath).Extension != ".lnk")
            {
                return null;
            }

            if (!System.IO.File.Exists(lnkPath))
            {
                return null;
            }
            string targetPath = ((IWshShortcut)shell.CreateShortcut(lnkPath)).TargetPath;
            foreach (string path in stopTargets)
            {   // 对于特殊 lnk 文件，将其视为真正文件
                if (targetPath.EndsWith(path))
                {
                    return null;
                }
            }

            if (Directory.Exists(targetPath) || System.IO.File.Exists(targetPath))
            {
                return targetPath;
            }
            else
            {
                return null;
            }
        }

        public static List<string> ListAll(string path, List<string> exts)
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
                foreach (string d in ListDirectory(path))
                {
                    res.Add(d);
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
