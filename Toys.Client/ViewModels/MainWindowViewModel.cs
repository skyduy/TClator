using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Toys.Client.Services;
using Toys.Client.Models;
using System.Diagnostics;
using System.Linq;

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

        private bool hasItems = false;
        public bool HasItems
        {
            get { return hasItems; }
            set
            {
                hasItems = value;
                this.RaisePropertyChanged(nameof(HasItems));
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
        public DelegateCommand DetailCommand { get; set; }

        public MainWindowViewModel()
        {
            ChangeSettingCommand = new DelegateCommand(new Action(ExecChangeSetting));
            ExitCommand = new DelegateCommand(new Action(ExecExit));
            DetailCommand = new DelegateCommand(new Action(ExecDetail));
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
            // noting todo backend.
        }

        private void ExecDetail()
        {
            // TODO
            //   1. Bind
            //   2. Exec
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
            HasItems = ResultList.Count > 0;
        }
    }
}
