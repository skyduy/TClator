using Newtonsoft.Json;
using System.IO;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class YoudaoSettingService
    {
        readonly string fn = @"config.json";

        public YoudaoSetting LoadSetting()
        {
            if (File.Exists(fn))
            {
                using StreamReader r = new StreamReader(fn);
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<YoudaoSetting>(json);
            }
            else
            {
                return new YoudaoSetting("", "");
            }
        }

        public void SaveSetting(YoudaoSetting setting)
        {
            string json = JsonConvert.SerializeObject(setting);
            File.WriteAllText(fn, json);
        }
    }
}
