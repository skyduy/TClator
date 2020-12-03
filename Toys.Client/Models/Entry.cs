using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toys.Client.Models
{
    class CommonEntry
    {
        public string Type => GetType().Name;
        public string Display { get; set; } = "";
    }

    class CalculateEntry : CommonEntry
    {
        public CalculateEntry() { }
        public CalculateEntry(string display)
        {
            Display = display;
        }
    }

    class TranslateEntry : CommonEntry
    {
        public TranslateEntry() { }
        public TranslateEntry(string display)
        {
            Display = display;
        }
    }

    class SearchEntry : CommonEntry
    {
        public string Match { get; set; } = "";
        public string Url { get; set; } = "";

        public SearchEntry() { }
        public SearchEntry(string fn, string extension, string url)
        {
            Match = (fn + " " + Path.GetFileNameWithoutExtension(url) + " " + extension).ToLower();
            Display = "[" + extension.ToUpper() + "] " + fn;
            Url = url;
        }
    }
}
