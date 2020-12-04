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
        public string Match { get; set; } = "";
        public string Url { get; set; } = "";

        public SearchEntry(string fn, string extension, string url)
        {
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

            Match = (fn + " " + Path.GetFileNameWithoutExtension(url) + " " + extension).ToLower();
            Display = fn;
            Url = url;

            DefaultActionIdx = 0;
            SecondActionIdx = 2;
        }

        public void Run()
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + Url + "\"";
            fileopener.Start();
        }

        public void CopyPath()
        {
            Clipboard.SetText(Url);
        }

        public void ShowInExplorer()
        {
            string folder = Path.GetDirectoryName(Url);
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + folder + "\"";
            fileopener.Start();
        }
    }
}
