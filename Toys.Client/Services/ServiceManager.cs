using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class ServiceManager
    {
        private readonly string settingFilename = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Toys", "config.json");
        private readonly FileSystemWatcher settingWatcher;
        private ICalculateService calculator;
        private ITranslateService translator;
        private ISearchService searcher;

        public ServiceManager()
        {
            settingWatcher = new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(settingFilename),
                Filter = Path.GetFileName(settingFilename),
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            settingWatcher.Changed += ReloadConfig;
            ReloadConfig(null, null);
        }

        public void OpenSettingFile()
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + settingFilename + "\"";
            fileopener.Start();
        }

        public List<CommonEntry> Search(string content)
        {
            List<CommonEntry> resultList = new List<CommonEntry>();
            foreach (SearchEntry entry in searcher.Search(content))
            {
                resultList.Add(entry);
            }

            if ("0123456789-.(（".Contains(content[0]))
            {
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
            return resultList;
        }

        private void ReloadConfig(object source, FileSystemEventArgs e)
        {
            Setting config = SettingServices.Load(settingFilename);
            if (config != null)
            {
                calculator = new NaiveCalculateService(config.CalculateConfig);
                translator = new YoudaoTranslateService(config.TranslateConfig);
                if (searcher == null)
                {
                    searcher = new WindowsSearchService(config.SearchConfig);
                }
                else
                {
                    searcher.Reload(config.SearchConfig);
                }
            }
        }
    }
}
