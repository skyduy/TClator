using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using Toys.Client.Services;

namespace Toys.Client.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        private BackgroundWorker bgw;

        readonly ICalculateService calculator = new NaiveCalculateService();
        readonly ITranslateService translator = new YoudaoTranslateService();

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
            get { return hasItems = false; }
            set
            {
                hasItems = value;
                this.RaisePropertyChanged(nameof(HasItems));
            }
        }

        private ObservableCollection<string> resultList = new ObservableCollection<string>();
        public ObservableCollection<string> ResultList
        {
            get { return resultList; }
            set
            {
                resultList = value;
                this.RaisePropertyChanged(nameof(ResultList));
            }
        }

        public YoudaoSettingViewModel YoudaoSetting { set; get; } = new YoudaoSettingViewModel();

        public DelegateCommand SendInputCMD { get; set; }

        public MainWindowViewModel()
        {
            SendInputCMD = new DelegateCommand(new Action(SendInputCMDExecute));
        }

        private void Query(object sender, DoWorkEventArgs e)
        {
            string content = (string)e.Argument;
            List<string> resultList = new List<string>();
            BackgroundWorker worker = sender as BackgroundWorker;
            while (!worker.CancellationPending)
            {
                if (content != string.Empty)
                {

                    if (char.IsDigit(currentText[0]) || currentText[0] == '-')
                    {
                        resultList.Add(calculator.Calculate("0" + currentText));
                    }
                    else
                    {
                        foreach (string entry in translator.Translate(currentText, YoudaoSetting.Setting))
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

            foreach (string item in (List<string>)e.Result)
            {
                ResultList.Add(item);
            }
            HasItems = ResultList.Count > 0;
        }

        private void SendInputCMDExecute()
        {

        }
    }
}
