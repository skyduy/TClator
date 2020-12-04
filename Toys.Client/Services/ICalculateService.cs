using System.Collections.Generic;
using Toys.Client.Models;

namespace Toys.Client.Services
{
    interface ICalculateService
    {
        List<CalculateEntry> Calculate(string question);
    }
}
