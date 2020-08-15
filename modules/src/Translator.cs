using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace TClator
{
    public class TranslateResult
    {
        public int ErrorCode { get; set; }
        public List<string> Translation { get; set; }
        public BasicTranslation Basic { get; set; }
        public List<WebTranslation> Web { get; set; }
    }

    public class BasicTranslation
    {
        public string Phonetic { get; set; }
        public List<string> Explains { get; set; }
    }

    public class WebTranslation
    {
        public string Key { get; set; }
        public List<string> Value { get; set; }
    }

    internal class Translator
    {
        // TODO 可优化：使用字典存储
        private List<string> cache;

        private string lastValid = "";
        private readonly string url = "https://openapi.youdao.com/api";
        private readonly Dictionary<String, String> dic = new Dictionary<String, String>();

        public List<string> Response(string q, string appKey, string appSecret)
        {
            if (q == lastValid)
            {
                return cache;
            }
            this.ConstructDict(q, appKey, appSecret);
            List<string> answers = new List<string>();

            string data;
            try
            {
                data = this.Fetch();
            }
            catch (Exception e)
            {
                answers.Add("[请求超时]" + e.Message);
                return answers;
            }

            if (data == string.Empty)
            {
                return answers;
            }

            TranslateResult o = JsonConvert.DeserializeObject<TranslateResult>(data);
            if (o.ErrorCode == 0)
            {
                if (o.Translation != null)
                {
                    var translation = string.Join(", ", o.Translation.ToArray());
                    var title = translation;
                    if (o.Basic?.Phonetic != null)
                    {
                        title += " [" + o.Basic.Phonetic + "]";
                    }
                    //answers.Add("[简] " + title);
                    answers.Add(title);
                }

                if (o.Basic?.Explains != null)
                {
                    var explantion = string.Join(",", o.Basic.Explains.ToArray());
                    //answers.Add("[译] " + explantion);
                    answers.Add(explantion);
                }

                if (o.Web != null)
                {
                    foreach (WebTranslation t in o.Web)
                    {
                        var translation = string.Join(",", t.Value.ToArray());
                        //answers.Add("[网] " + translation);
                        answers.Add(translation);
                    }
                }
                lastValid = q;
                cache = answers;
            }
            else
            {
                string error;
                switch (o.ErrorCode)
                {
                    case 108:
                        error = "[有道智云] 应用ID不正确（请右击托盘图标设置）";
                        break;

                    case 202:
                        error = "[有道智云] 签名检验失败（请右击托盘图标设置）";
                        break;

                    default:
                        error = "[有道智云] 错误代码" + o.ErrorCode;
                        break;
                }
                answers.Add(error);
            }
            return answers;
        }

        private void ConstructDict(string q, string appKey, string appSecret)
        {
            this.dic.Clear();
            string salt = DateTime.Now.Millisecond.ToString();
            dic.Add("from", "auto");
            dic.Add("to", "auto");
            dic.Add("signType", "v3");
            TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            long millis = (long)ts.TotalMilliseconds;
            string curtime = Convert.ToString(millis / 1000);
            dic.Add("curtime", curtime);
            string signStr = appKey + Truncate(q) + salt + curtime + appSecret; ;
            string sign = ComputeHash(signStr, new SHA256CryptoServiceProvider());
            dic.Add("q", Uri.EscapeUriString(q));
            dic.Add("appKey", appKey);
            dic.Add("salt", salt);
            dic.Add("sign", sign);
        }

        protected string Fetch()
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            StringBuilder builder = new StringBuilder();
            int i = 0;
            foreach (var item in dic)
            {
                if (i > 0)
                    builder.Append("&");
                builder.AppendFormat("{0}={1}", item.Key, item.Value);
                i++;
            }
            byte[] data = Encoding.UTF8.GetBytes(builder.ToString());
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
            }
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            if (resp.ContentType.ToLower().Equals("audio/mp3"))
            {
                return string.Empty;
            }
            else
            {
                Stream stream = resp.GetResponseStream();
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                return result;
            }
        }

        protected static string ComputeHash(string input, HashAlgorithm algorithm)
        {
            Byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            Byte[] hashedBytes = algorithm.ComputeHash(inputBytes);
            return BitConverter.ToString(hashedBytes).Replace("-", "");
        }

        protected static string Truncate(string q)
        {
            if (q == null)
            {
                return null;
            }
            int len = q.Length;
            return len <= 20 ? q : (q.Substring(0, 10) + len + q.Substring(len - 10, 10));
        }
    }
}
