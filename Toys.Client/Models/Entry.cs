using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Toys.Client.Services;
using System.Windows.Media.Imaging;

namespace Toys.Client.Models
{
    class EntryAction
    {
        public string Name { get; set; }
        public Action Run { get; set; }
        public Func<Tuple<string, string>> Detail { get; set; }
    }

    class CommonEntry
    {
        public string Src { get; set; } = "";
        public string Display { get; set; } = "";
        public BitmapSource ImageData { get; set; }

        public List<EntryAction> ActionList { get; set; } = new List<EntryAction>();
        public int DefaultActionIdx;
        public int SecondActionIdx;

        public string Type => GetType().Name;
    }

    class CalculateEntry : CommonEntry
    {
        public CalculateEntry(string display)
        {
            Display = display;

            ActionList.Add(new EntryAction()
            {
                Name = "Copy",
                Run = new Action(() =>
                {
                    Clipboard.SetText(Display);
                })
            });
            DefaultActionIdx = 0;
            DefaultActionIdx = 0;
        }
    }

    class TranslateEntry : CommonEntry
    {
        public TranslateEntry(string src, string display)
        {
            Src = src;
            Display = display;

            ActionList.Add(new EntryAction()
            {
                Name = "Copy",
                Run = new Action(() =>
                {
                    Clipboard.SetText(Display);
                })
            });
            ActionList.Add(new EntryAction()
            {
                Name = "Detail",
                Detail = new Func<Tuple<string, string>>(() =>
                {
                    return new Tuple<string, string>(Src, Display);
                })
            });
            DefaultActionIdx = 1;
            SecondActionIdx = 1;
        }
    }

    class SearchEntry : CommonEntry
    {
        public int Count { get; set; } = 0;
        public string FullPath { get; set; } = "";
        public List<string> Aliases { get; } = new List<string>();

        public SearchEntry(string fullPath, string alias)
        {
            fullPath = fullPath.Replace('/', '\\');
            Count = 1;
            FullPath = fullPath;
            string filename = Path.GetFileNameWithoutExtension(FullPath);
            Aliases.Add(filename.ToLower());
            if (alias != null)
            {
                Display = alias;
                Aliases.Add(alias.ToLower());
            }
            else
            {
                Display = filename;
            }

            ActionList.Add(new EntryAction()
            {
                Name = "Run",
                Run = new Action(() =>
                {
                    Process fileopener = new Process();
                    fileopener.StartInfo.FileName = "explorer";
                    fileopener.StartInfo.Arguments = "\"" + FullPath + "\"";
                    fileopener.Start();
                    SearchHistory.Increase(FullPath);
                })
            });
            ActionList.Add(new EntryAction()
            {
                Name = "Copy Path",
                Run = new Action(() =>
                {
                    Clipboard.SetText(FullPath);
                })
            });
            ActionList.Add(new EntryAction()
            {
                Name = "Show in Explorer",
                Run = new Action(() =>
                {
                    string folder = Path.GetDirectoryName(FullPath);
                    Process fileopener = new Process();
                    fileopener.StartInfo.FileName = "explorer";
                    fileopener.StartInfo.Arguments = "\"" + folder + "\"";
                    fileopener.Start();
                })
            });
            DefaultActionIdx = 0;
            SecondActionIdx = 2;
        }
    }
}
