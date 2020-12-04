using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Toys.Client.Services;
using Toys.Client.Models;
using Toys.Client.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;

namespace Toys.Client.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        private BackgroundWorker bgw;

        static private readonly string settingFilename = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Toys", "config.json");
        public Setting Config { get; set; } = SettingServices.Load(settingFilename);

        readonly ICalculateService calculator;
        readonly ITranslateService translator;
        readonly ISearchService searcher;

        // viewmodels
        public TranslateResultDetailViewModel DetailViewModel { get; } = new TranslateResultDetailViewModel();

        // binding data
        private string currentText = "";
        public string CurrentText
        {
            get { return currentText; }
            set
            {
                currentText = value;
                RaisePropertyChanged(nameof(CurrentText));
                ResultList.Clear();

                if (currentText.Trim() != "")
                {
                    if (bgw == null || (bgw.WorkerSupportsCancellation && bgw.IsBusy))
                    {
                        if (bgw != null)
                        {
                            bgw.CancelAsync();
                        }
                        bgw = new BackgroundWorker
                        {
                            WorkerSupportsCancellation = true
                        };
                        bgw.DoWork += new DoWorkEventHandler(Query);
                        bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(QueryCallback);
                    }
                    bgw.RunWorkerAsync(currentText.Trim());
                }
            }
        }

        private ObservableCollection<CommonEntry> resultList = new ObservableCollection<CommonEntry>();
        public ObservableCollection<CommonEntry> ResultList
        {
            get { return resultList; }
            set
            {
                resultList = value;
                this.RaisePropertyChanged(nameof(ResultList));
            }
        }

        // binding command
        public DelegateCommand ChangeSettingCommand { get; set; }
        public DelegateCommand ExitCommand { get; set; }
        public DelegateCommand<CommonEntry> CopyCommand { get; set; }
        public DelegateCommand<CommonEntry> DefaultActionCommand { get; set; }
        public DelegateCommand ActivateCommand { get; set; }
        public DelegateCommand<EntryAction> ActionCommand { get; set; }

        // manual delegate 
        public Action ShowDetailAction { get; set; }
        public Action ActivateMainWindowAction { get; set; }

        // c'tor
        public MainWindowViewModel()
        {
            calculator = new NaiveCalculateService(Config.CalculateConfig);
            translator = new YoudaoTranslateService(Config.TranslateConfig);
            if (OperatingSystem.IsWindows())
            {
                searcher = new WindowsSearchSearvice(Config.SearchConfig);
            }

            ChangeSettingCommand = new DelegateCommand(new Action(ExecChangeSetting));
            ExitCommand = new DelegateCommand(new Action(ExecExit));
            CopyCommand = new DelegateCommand<CommonEntry>(new Action<CommonEntry>(ExecCopy));
            DefaultActionCommand = new DelegateCommand<CommonEntry>(new Action<CommonEntry>(ExecDefaultAction));
            ActivateCommand = new DelegateCommand(new Action(ExecActivate));
            ActionCommand = new DelegateCommand<EntryAction>(new Action<EntryAction>(ExecAction));
        }

        // functions
        private void ExecChangeSetting()
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + settingFilename + "\"";
            fileopener.Start();
        }

        private void ExecExit()
        {
            Debug.Print("Exit");
        }

        private void ExecCopy(CommonEntry entry)
        {
            Clipboard.SetText(entry.Display);
        }

        private void ExecDefaultAction(CommonEntry entry)
        {
            Debug.Print("exec default action");
            ExecAction(entry.ActionList[entry.DefaultActionIdx]);
        }

        private void ExecActivate()
        {
            ActivateMainWindowAction?.Invoke();
        }

        private void ExecAction(EntryAction action)
        {
            if (action.Detail != null)
            {
                (DetailViewModel.Src, DetailViewModel.Dst) = action.Detail();
                ShowDetailAction?.Invoke();
            }
            action.Run?.Invoke();
        }

        private void Query(object sender, DoWorkEventArgs e)
        {
            string content = (string)e.Argument;
            List<CommonEntry> resultList = new List<CommonEntry>();
            BackgroundWorker worker = sender as BackgroundWorker;
            while (!worker.CancellationPending)
            {
                if (content != string.Empty)
                {
                    if (searcher != null)
                    {
                        foreach (SearchEntry entry in searcher.Search(content))
                        {
                            resultList.Add(entry);
                        }
                    }
                    if ("0123456789-.(（".Contains(content[0]))
                    {
                        if (content[0] == '.')
                        {
                            content = "0" + content;
                        }
                        content = content.Replace('（', '(').Replace('）', ')');
                        content = content.Replace('、', '/').Replace('《', '<');
                        content = content.Replace("**", "^").Replace("<<", "*2^");
                        foreach (CalculateEntry entry in calculator.Calculate(content))
                        {
                            resultList.Add(entry);
                        }
                    }
                    else
                    {
                        foreach (TranslateEntry entry in translator.Translate(content))
                        {
                            resultList.Add(entry);
                        }
                    }
                    e.Result = resultList;
                }
                break;
            }
            if (worker.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        private void QueryCallback(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled || e.Result is null)
            {
                return;
            }

            foreach (CommonEntry item in (List<CommonEntry>)e.Result)
            {
                switch (item.Type)
                {
                    case "CalculateEntry":
                        item.ImageData = new BitmapImage(new Uri(@"Assets\calculator.ico", UriKind.Relative));
                        break;
                    case "TranslateEntry":
                        item.ImageData = new BitmapImage(new Uri(@"Assets\youdao.ico", UriKind.Relative));
                        break;
                    case "SearchEntry":
                        var sysicon = Icon.ExtractAssociatedIcon(((SearchEntry)item).Url);
                        var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(
                                    sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        sysicon.Dispose();
                        item.ImageData = bmpSrc;
                        break;
                }
                ResultList.Add(item);
            }
        }
    }
}
