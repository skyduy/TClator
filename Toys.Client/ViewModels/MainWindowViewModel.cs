using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Toys.Client.Services;
using Toys.Client.Models;
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
        private readonly ServiceManager sm = new ServiceManager();

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
        public DelegateCommand ActivateCommand { get; set; }
        public DelegateCommand ExitCommand { get; set; }
        public DelegateCommand ChangeSettingCommand { get; set; }
        public DelegateCommand<CommonEntry> CopyCommand { get; set; }
        public DelegateCommand<CommonEntry> DefaultActionCommand { get; set; }
        public DelegateCommand<CommonEntry> SecondActionCommand { get; set; }
        public DelegateCommand<EntryAction> ActionCommand { get; set; }

        // manual delegate 
        public Action ShowDetailAction { get; set; }
        public Action ActivateMainWindowAction { get; set; }

        // c'tor
        public MainWindowViewModel()
        {
            ActivateCommand = new DelegateCommand(new Action(() =>
            {
                ActivateMainWindowAction?.Invoke();
            }));
            ExitCommand = new DelegateCommand(new Action(() =>
            {
                Debug.Print("Exit");
            }));
            ChangeSettingCommand = new DelegateCommand(new Action(() =>
            {
                sm.OpenSettingFile();
            }));
            CopyCommand = new DelegateCommand<CommonEntry>(new Action<CommonEntry>((CommonEntry entry) =>
            {
                Clipboard.SetText(entry.Display);
            }));

            DefaultActionCommand = new DelegateCommand<CommonEntry>(new Action<CommonEntry>(ExecDefaultAction));
            SecondActionCommand = new DelegateCommand<CommonEntry>(new Action<CommonEntry>(ExecSecondAction));
            ActionCommand = new DelegateCommand<EntryAction>(new Action<EntryAction>(ExecAction));
        }

        // command functions
        private void ExecDefaultAction(CommonEntry entry)
        {
            ExecAction(entry.ActionList[entry.DefaultActionIdx]);
        }

        private void ExecSecondAction(CommonEntry entry)
        {
            ExecAction(entry.ActionList[entry.SecondActionIdx]);
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
            BackgroundWorker worker = sender as BackgroundWorker;
            while (!worker.CancellationPending)
            {
                if (content != string.Empty)
                {
                    e.Result = sm.Search(content);
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
                        item.ImageData = new BitmapImage(new Uri(@"..\Assets\calculator.ico", UriKind.Relative));
                        break;
                    case "TranslateEntry":
                        item.ImageData = new BitmapImage(new Uri(@"..\Assets\youdao.ico", UriKind.Relative));
                        break;
                    case "SearchEntry":
                        var sysicon = Icon.ExtractAssociatedIcon(((SearchEntry)item).FullPath);
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
