using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class WindowsSearchSearvice : ISearchService
    {
        readonly OleDbConnection conn = new OleDbConnection(@"Provider=Search.CollatorDSO;Extended Properties=""Application=Windows""");
        public WindowsSearchSearvice()
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
        }

        public List<SearchEntry> Search(string keyword, SearchSetting setting)
        {
            if (!setting.Enable || setting.SearchPaths.Count == 0) return default;

            List<string> scopesList = new List<string>();
            foreach (string fn in setting.SearchPaths)
            {
                scopesList.Add(string.Format(@"scope='file:{0}'", fn));
            }
            string scopesCondition = "(" + string.Join(" OR ", scopesList) + ")";
            string query = string.Format(
                @"SELECT System.ItemNameDisplayWithoutExtension, System.ItemUrl FROM SystemIndex " +
                @"WHERE {0} AND System.ItemType != 'Directory' AND System.ItemNameDisplayWithoutExtension LIKE '%{1}%'",
                scopesCondition, keyword);

            OleDbCommand command = new OleDbCommand(query, conn);
            try
            {
                conn.Open();
                var r = command.ExecuteReader();
                List<SearchEntry> res = new List<SearchEntry>();
                while (r.Read())
                {
                    string fn = r[0].ToString();
                    string url = r[1].ToString();
                    if (!fn.Contains("卸载"))
                    {
                        res.Add(new SearchEntry(fn, url));
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return default;
            }
            finally
            {
                conn.Close();
            }
        }

        public bool Open(SearchEntry entry)
        {
            if (entry.Url.StartsWith("file:"))
            {
                string linkPathName = entry.Url.ToString()[5..];
                if (System.IO.File.Exists(linkPathName))
                {
                    // WshShellClass shell = new WshShellClass();
                    WshShell shell = new WshShell(); //Create a new WshShell Interface
                    IWshShortcut link = (IWshShortcut)shell.CreateShortcut(linkPathName); //Link the interface to our shortcut
                    Process.Start(link.TargetPath);
                }
            }
            return false;
        }
    }
}
