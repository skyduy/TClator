using System.Collections.Generic;

namespace Toys.Client.Models
{
    class Setting
    {
        public SearchSetting SearchConfig { get; set; }
        public TranslateSetting TranslateConfig { get; set; }
        public CalculateSetting CalculateConfig { get; set; }

        public Setting()
        {
            SearchConfig = new SearchSetting();
            TranslateConfig = new TranslateSetting();
            CalculateConfig = new CalculateSetting();
        }
    }

    class SearchSetting : CommonSetting
    {
        public List<string> SearchPaths { get; set; } = new List<string>();
    }

    class TranslateSetting : CommonSetting
    {
        public string YoudaoAppKey { get; set; } = "";
        public string YoudaoAppSecret { get; set; } = "";
    }

    class CalculateSetting : CommonSetting
    {

    }

    class CommonSetting
    {
        public bool Enable { get; set; } = true;
    }
}
