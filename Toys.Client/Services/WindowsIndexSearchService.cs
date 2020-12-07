using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class WindowsIndexSearchService : ISearchService
    {
        readonly Dictionary<string, string> lnk2real = new Dictionary<string, string>();
        readonly Dictionary<string, SearchEntry> refCount = new Dictionary<string, SearchEntry>();

        public WindowsIndexSearchService(SearchSetting setting)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            if (!setting.Enable || setting.SearchPaths.Count == 0) return;

            List<string> extensions = null;
            if (!setting.Extensions.Contains("*") && setting.Extensions.Count != 0)
            {
                extensions = new List<string>();
                foreach (string s in setting.Extensions)
                {
                    extensions.Add(s.ToLower());
                }
            }

            List<string> scopesList = new List<string>();
            foreach (string fn in setting.SearchPaths)
            {
                scopesList.Add(string.Format(@"scope='file:{0}'", fn));
            }
            string scopesCondition = "(" + string.Join(" OR ", scopesList) + ")";
            string query = string.Format(
                @"SELECT System.ItemNameDisplayWithoutExtension, System.ItemUrl FROM SystemIndex WHERE {0} AND System.ItemType != 'Directory'",
                scopesCondition);

            OleDbConnection conn = new OleDbConnection(@"Provider=Search.CollatorDSO;Extended Properties=""Application=Windows""");
            OleDbCommand command = new OleDbCommand(query, conn);
            WshShell shell = new WshShell();
            try
            {
                conn.Open();
                var r = command.ExecuteReader();
                while (r.Read())
                {
                    string fn = r[0].ToString();
                    string url = r[1].ToString();
                    string path = url[5..];
                    if (fn.Contains("卸载") || fn.Contains("Uninstall") || !url.StartsWith("file:") || !System.IO.File.Exists(path)) continue;

                    if (path.EndsWith(".lnk"))
                    {
                        path = ((IWshShortcut)shell.CreateShortcut(path)).TargetPath;
                        if (!System.IO.File.Exists(path)) continue;
                    }
                    string extension = path;
                    int idx = path.LastIndexOf('.');
                    if (idx != -1)
                    {
                        extension = path[(idx + 1)..];
                    }

                    if (extensions == null || extensions.Contains(extension.ToLower()))
                    {
                        Increase(path, fn);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
            finally
            {
                conn.Close();
            }
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

        public List<SearchEntry> Search(string keyword)
        {
            string[] words = keyword.ToLower().Split(' ');
            List<SearchEntry> res = new List<SearchEntry>();
            foreach (var item in refCount)
            {
                if (item.Value.Match(words))
                {
                    res.Add(item.Value);
                }
            }
            return res;
        }
    }
}
