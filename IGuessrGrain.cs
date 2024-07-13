using Orleans;
using System.Threading.Tasks;

namespace Guessr
{
    public interface IGuessrGrain : IGrainWithGuidKey
    {
        Task<string> SayHello(string name);
    }
}