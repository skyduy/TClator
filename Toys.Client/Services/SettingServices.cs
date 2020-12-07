using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class SettingServices
    {
        public static Setting Load(string configFn)
        {
            if (!File.Exists(configFn))
            {
                Setting setting = new Setting();
                Dump(setting, configFn);
                return setting;
            }

            string json;
            using (FileStream fsRead = new FileStream(configFn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int fsLen = (int)fsRead.Length;
                Debug.Print("{0}", fsLen);
                byte[] heByte = new byte[fsLen];
                fsRead.Read(heByte, 0, heByte.Length);
                json = System.Text.Encoding.UTF8.GetString(heByte);
            }
            return JsonConvert.DeserializeObject<Setting>(json);
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
