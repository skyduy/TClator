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
        public class SeachPath
        {
            public string Path { get; set; }
            public int MaxDepth { get; set; }
        }

        public List<SeachPath> SearchPaths { get; set; } = new List<SeachPath>();
        public List<string> Extensions { get; set; } = new List<string>();
        public int MaxCount { get; set; } = 5;
    }

    class TranslateSetting : CommonSetting
    {
        public string YoudaoAppKey { get; set; } = "";
        public string YoudaoAppSecret { get; set; } = "";
        public int MaxCount { get; set; } = 5;
    }

    class CalculateSetting : CommonSetting
    {

    }

    class CommonSetting
    {
        public bool Enable { get; set; } = false;
    }
}
