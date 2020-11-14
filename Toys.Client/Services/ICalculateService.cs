using Toys.Client.Models;

namespace Toys.Client.Services
{
    interface ICalculateService
    {
        CalculateEntry Calculate(string question, CalculateSetting setting);
    }
}
