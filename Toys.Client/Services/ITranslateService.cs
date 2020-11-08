using System.Collections.Generic;

namespace Toys.Client.Services
{
    interface ITranslateService
    {
        List<string> Translate(string src, object options);
    }
}
