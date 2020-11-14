using System.Collections.Generic;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    interface ISearchService
    {
        List<SearchEntry> Search(string keyword, SearchSetting setting);
        bool Open(SearchEntry entry);
    }
}
