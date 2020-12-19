using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Toys.Client.Services;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Drawing;

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

        public List<EntryAction> ActionList { get; set; } = new List<EntryAction>();
        public int DefaultActionIdx;
        public int SecondActionIdx;

        public string Type => GetType().Name;

        // 必须 和 UI 同线程执行的任务
        public virtual void UISyncTask() { }
    }

    class CalculateEntry : CommonEntry
    {
        public static BitmapSource ImageData { get; } = new BitmapImage(new Uri(@"..\Assets\calculator.ico", UriKind.Relative));
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
        public static BitmapSource ImageData { get; } = new BitmapImage(new Uri(@"..\Assets\youdao.ico", UriKind.Relative));
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
        private static BitmapSource FolderImageData { get; }
        public BitmapSource ImageData { get; set; } = null;
        public string FullPath { get; set; } = "";

        static SearchEntry()
        {
            var sysicon = Icon.ExtractAssociatedIcon("C:\\WINDOWS\\explorer.exe");
            FolderImageData = Imaging.CreateBitmapSourceFromHIcon(
                        sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            sysicon.Dispose();
            FolderImageData.Freeze();
        }

        public SearchEntry(string fullPath, string display)
        {
            FullPath = fullPath.Replace('/', '\\');
            Display = display;

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

            if (File.Exists(FullPath))
            {
                var sysicon = Icon.ExtractAssociatedIcon(FullPath);
                ImageData = Imaging.CreateBitmapSourceFromHIcon(
                            sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                sysicon.Dispose();
                ImageData.Freeze();
            }
            else
            {
                ImageData = FolderImageData;
            }
        }

        public SearchEntry()
        {
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

        public override void UISyncTask()
        {
            if (ImageData == null)
            {
                ImageData = new BitmapImage(new Uri(@"..\Assets\loading.ico", UriKind.Relative));
            }
        }
    }
}
