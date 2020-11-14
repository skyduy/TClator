using System;
using System.Collections.Generic;
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
        public string Url { get; set; } = "";

        public SearchEntry() { }
        public SearchEntry(string display, string url)
        {
            Display = "[APP] " + display;
            Url = url;
        }
    }
}
