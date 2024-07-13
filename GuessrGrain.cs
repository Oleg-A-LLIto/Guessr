using System.Threading.Tasks;

namespace Guessr
{
    public class GuessrGrain : Grain, IGuessrGrain
    {
        public Task<string> SayHello(string name)
        {
            return Task.FromResult($"Hello, {name} from Orleans!");
        }
    }
}