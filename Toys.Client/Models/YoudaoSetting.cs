using System;
using System.Collections.Generic;
using System.Text;

namespace Toys.Client.Models
{
    class YoudaoSetting
    {
        public string AppKey { get; set; }
        public string AppSecret { get; set; }

        public YoudaoSetting(string appKey, string appSecret)
        {
            AppKey = appKey;
            AppSecret = appSecret;
        }
    }
}
