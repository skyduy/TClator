using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    class YoudaoTranslateService : ITranslateService
    {
        private readonly LRUCache<string, List<string>> cache = new LRUCache<string, List<string>>(1024);

        private string FetchYoudao(string q, YoudaoSetting s)
        {
            // construct params
            Dictionary<String, String> paramsDict = new Dictionary<String, String>();
            string salt = DateTime.Now.Millisecond.ToString();
            paramsDict.Add("from", "auto");
            paramsDict.Add("to", "auto");
            paramsDict.Add("signType", "v3");
            TimeSpan ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            long millis = (long)ts.TotalMilliseconds;
            string curtime = Convert.ToString(millis / 1000);
            paramsDict.Add("curtime", curtime);
            string signStr = s.AppKey + truncate(q) + salt + curtime + s.AppSecret; ;
            string sign = computeHash(signStr, new SHA256CryptoServiceProvider());
            paramsDict.Add("q", Uri.EscapeUriString(q));
            paramsDict.Add("appKey", s.AppKey);
            paramsDict.Add("salt", salt);
            paramsDict.Add("sign", sign);

            // fetch data
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://openapi.youdao.com/api");
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            StringBuilder builder = new StringBuilder();
            int i = 0;
            foreach (var item in paramsDict)
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

            static string computeHash(string input, HashAlgorithm algorithm)
            {
                Byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                Byte[] hashedBytes = algorithm.ComputeHash(inputBytes);
                return BitConverter.ToString(hashedBytes).Replace("-", "");
            }

            static string truncate(string q)
            {
                if (q == null)
                {
                    return null;
                }
                int len = q.Length;
                return len <= 20 ? q : (q.Substring(0, 10) + len + q.Substring(len - 10, 10));
            }
        }

        public List<string> Translate(string src, object options)
        {
            // query cache
            List<string> answers = cache.Get(src);
            if (answers != null)
            {
                return answers;
            }
            answers = new List<string>();

            // fetch data
            string data;
            try
            {
                data = this.FetchYoudao(src, (YoudaoSetting)options);
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

            // parse data
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
                    answers.Add("[简] " + title);
                }

                if (o.Basic?.Explains != null)
                {
                    var explantion = string.Join(",", o.Basic.Explains.ToArray());
                    answers.Add("[译] " + explantion);
                }

                if (o.Web != null)
                {
                    foreach (WebTranslation t in o.Web)
                    {
                        var translation = string.Join(",", t.Value.ToArray());
                        answers.Add("[网] " + translation);
                    }
                }
                cache.Add(src, answers);
            }
            else
            {
                string error = o.ErrorCode switch
                {
                    108 => "[有道智云] 应用ID不正确（请右击托盘图标设置）",
                    202 => "[有道智云] 签名检验失败（请右击托盘图标设置）",
                    _ => "[有道智云] 错误代码" + o.ErrorCode,
                };
                answers.Add(error);
            }
            return answers;
        }
    }

    class TranslateResult
    {
        public int ErrorCode { get; set; }
        public List<string> Translation { get; set; }
        public BasicTranslation Basic { get; set; }
        public List<WebTranslation> Web { get; set; }
    }

    class BasicTranslation
    {
        public string Phonetic { get; set; }
        public List<string> Explains { get; set; }
    }

    class WebTranslation
    {
        public string Key { get; set; }
        public List<string> Value { get; set; }
    }

    class LRUCache<K, V>
    {
        private readonly int capacity;
        private readonly Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>> cacheMap = new Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>>();
        private readonly LinkedList<LRUCacheItem<K, V>> lruList = new LinkedList<LRUCacheItem<K, V>>();

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public V Get(K key)
        {
            if (cacheMap.TryGetValue(key, out LinkedListNode<LRUCacheItem<K, V>> node))
            {
                V value = node.Value.value;
                lruList.Remove(node);
                lruList.AddLast(node);
                return value;
            }
            return default;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(K key, V val)
        {
            if (cacheMap.Count >= capacity)
            {
                RemoveFirst();
            }

            LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(key, val);
            LinkedListNode<LRUCacheItem<K, V>> node = new LinkedListNode<LRUCacheItem<K, V>>(cacheItem);
            lruList.AddLast(node);
            cacheMap.Add(key, node);
        }

        private void RemoveFirst()
        {
            // Remove from LRUPriority
            LinkedListNode<LRUCacheItem<K, V>> node = lruList.First;
            lruList.RemoveFirst();

            // Remove from cache
            cacheMap.Remove(node.Value.key);
        }
    }

    class LRUCacheItem<K, V>
    {
        public LRUCacheItem(K k, V v)
        {
            key = k;
            value = v;
        }
        public K key;
        public V value;
    }
}
