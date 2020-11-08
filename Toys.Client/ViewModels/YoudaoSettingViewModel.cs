using Prism.Commands;
using Prism.Mvvm;
using System;
using Toys.Client.Models;
using Toys.Client.Services;

namespace Toys.Client.ViewModels
{
    class YoudaoSettingViewModel : BindableBase
    {
        YoudaoSettingService SettingService { get; set; } = new YoudaoSettingService();
        public YoudaoSetting Setting { get; set; }

        public YoudaoSettingViewModel()
        {
            Setting = SettingService.LoadSetting();
            SaveCMD = new DelegateCommand(new Action(SaveCMDExecute));
        }

        public DelegateCommand SaveCMD { get; set; }
        private void SaveCMDExecute()
        {
            SettingService.SaveSetting(Setting);
        }
    }
}
