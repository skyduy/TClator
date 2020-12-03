using Newtonsoft.Json;
using System.IO;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class SettingServices
    {
        public static Setting Load(string configFn)
        {
            if (File.Exists(configFn))
            {
                using StreamReader r = new StreamReader(configFn);
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<Setting>(json);
            }
            else
            {
                Setting setting = new Setting();
                Dump(setting, configFn);
                return setting;
            }
        }

        public static void Dump(Setting setting, string configFn)
        {
            string json = JsonConvert.SerializeObject(setting);
            if (!File.Exists(configFn))
            {
                FileInfo file_info = new FileInfo(configFn);
                Directory.CreateDirectory(file_info.DirectoryName);
            }
            File.WriteAllText(configFn, json);
        }
    }
}
