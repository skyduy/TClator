using System.Collections.Generic;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    interface ITranslateService
    {
        List<TranslateEntry> Translate(string src);
    }
}
