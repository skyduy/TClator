using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
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
        public string Src { get; set; }

        public string Type => GetType().Name;
        public BitmapSource ImageData { get; set; }
        public string Display { get; set; } = "";

        public int DefaultActionIdx;
        public int SecondActionIdx;
        public List<EntryAction> ActionList { get; set; } = new List<EntryAction>();
    }

    class CalculateEntry : CommonEntry
    {
        public CalculateEntry(string display)
        {
            ActionList.Add(new EntryAction()
            {
                Name = "Copy",
                Run = new Action(Copy)
            });

            Display = display;
            DefaultActionIdx = 0;
            DefaultActionIdx = 0;
        }

        public void Copy()
        {
            Clipboard.SetText(Display);
        }
    }

    class TranslateEntry : CommonEntry
    {
        public TranslateEntry(string display)
        {
            ActionList.Add(new EntryAction()
            {
                Name = "Copy",
                Run = new Action(Copy)
            });
            ActionList.Add(new EntryAction()
            {
                Name = "Detail",
                Detail = new Func<Tuple<string, string>>(Detail)
            });
            Display = display;
            DefaultActionIdx = 1;
            SecondActionIdx = 1;
        }

        public void Copy()
        {
            Clipboard.SetText(Display);
        }

        public Tuple<string, string> Detail()
        {
            return new Tuple<string, string>(Src, Display);
        }
    }

    class SearchEntry : CommonEntry
    {
        public int Count { get; set; } = 0;
        public string FullPath { get; set; } = "";
        public List<string> Matches { get; } = new List<string>();

        public bool Match(string word)
        {
            foreach (string t in Matches)
            {
                if (t.ToLower().Contains(word.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Match(string[] words)
        {
            foreach (string word in words)
            {
                if (Match(word))
                {
                    return true;
                }
            }
            return false;
        }

        public SearchEntry(string fullPath, string alias)
        {
            fullPath = fullPath.Replace('/', '\\');
            ActionList.Add(new EntryAction()
            {
                Name = "Run",
                Run = new Action(Run)
            });
            ActionList.Add(new EntryAction()
            {
                Name = "Copy Path",
                Run = new Action(CopyPath)
            });
            ActionList.Add(new EntryAction()
            {
                Name = "Show in Explorer",
                Run = new Action(ShowInExplorer)
            });

            Count = 1;
            FullPath = fullPath;
            string filename = Path.GetFileNameWithoutExtension(FullPath);
            Matches.Add(filename);
            if (alias != null)
            {
                Display = alias;
                Matches.Add(alias);
            }
            else
            {
                Display = filename;
            }

            DefaultActionIdx = 0;
            SecondActionIdx = 2;
        }

        public void Run()
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + FullPath + "\"";
            fileopener.Start();
        }

        public void CopyPath()
        {
            Clipboard.SetText(FullPath);
        }

        public void ShowInExplorer()
        {
            string folder = Path.GetDirectoryName(FullPath);
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + folder + "\"";
            fileopener.Start();
        }
    }
}
