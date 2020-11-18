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
using System.Linq;
using System.Windows;

namespace Toys.Client.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        private BackgroundWorker bgw;

        readonly ICalculateService calculator = new NaiveCalculateService();
        readonly ITranslateService translator = new YoudaoTranslateService();
        readonly ISearchService searcher = new WindowsSearchSearvice();

        static private readonly string settingFilename = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Toys", "config.json");
        public Setting Config { get; set; } = SettingServices.Load(settingFilename);

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

        public DelegateCommand ChangeSettingCommand { get; set; }
        public DelegateCommand ExitCommand { get; set; }
        public DelegateCommand<CommonEntry> CopyCommand { get; set; }
        public DelegateCommand<CommonEntry> DetailCommand { get; set; }

        public MainWindowViewModel()
        {
            ChangeSettingCommand = new DelegateCommand(new Action(ExecChangeSetting));
            ExitCommand = new DelegateCommand(new Action(ExecExit));
            CopyCommand = new DelegateCommand<CommonEntry>(new Action<CommonEntry>(ExecCopy));
            DetailCommand = new DelegateCommand<CommonEntry>(new Action<CommonEntry>(ExecDetail));
        }

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

        private void ExecDetail(CommonEntry entry)
        {
            Debug.Print(entry.Type);
            if (entry.Type == nameof(CalculateEntry))
            {
                Clipboard.SetText(entry.Display);
            }
            else if (entry.Type == nameof(TranslateEntry))
            {
                ResultDetailView window = new ResultDetailView(currentText.Trim(), entry.Display);
                window.ShowDialog();
            }
            else if (entry.Type == nameof(SearchEntry))
            {
                searcher.Open((SearchEntry)entry);
            }
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

                    if (char.IsDigit(currentText[0]) || currentText[0] == '-')
                    {
                        resultList.Add(calculator.Calculate("0" + currentText, Config.CalculateConfig));
                    }
                    else
                    {
                        foreach (SearchEntry entry in searcher.Search(currentText, Config.SearchConfig) ??
                            Enumerable.Empty<SearchEntry>())
                        {
                            resultList.Add(entry);
                        }

                        foreach (TranslateEntry entry in translator.Translate(currentText, Config.TranslateConfig))
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
                ResultList.Add(item);
            }
        }
    }
}
