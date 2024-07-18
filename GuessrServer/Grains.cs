using Orleans.Providers;

namespace Guessr.Grains
{
    public interface IPlayerGrain : IGrainWithGuidKey
    {
        Task JoinQueue();
        Task JoinRoom(Guid roomId);
        Task<int> MakeGuess(int guess); // 0 - game not concluded, 1 - you win, 2 - you lose, 3 - draw, also 4 is reserved for invalid inputs.
        Task<int> GetPoints();
        Task<int> GetResult();
        Task AddPoint();
        Task LeaveRoom();
        Task<Guid> GetRoomId();
    }

    [Serializable]
    public class PlayerScore
    {
        public int Score { get; set; }
    }

    [StorageProvider(ProviderName = "PlayerStore")]
    public class PlayerGrain : Grain<PlayerScore>, IPlayerGrain
    {
        private Guid _currentRoomId = Guid.Empty;

        public async Task<int> GetResult()
        {
            IRoomGrain roomGrain = GrainFactory.GetGrain<IRoomGrain>(_currentRoomId);
            return await roomGrain.GetResult(this.GetPrimaryKey());
        }

        public Task JoinQueue()
        {
            Console.WriteLine($"Player {this.GetPrimaryKey()} joined the queue.");
            _currentRoomId = Guid.Empty;
            return Task.CompletedTask;
        }

        public Task JoinRoom(Guid roomId)
        {
            Console.WriteLine($"Player {this.GetPrimaryKey()} joined room {roomId}.");
            _currentRoomId = roomId;
            return Task.CompletedTask;
        }

        public async Task<int> MakeGuess(int guess)
        {
            Console.WriteLine($"Player {this.GetPrimaryKey()} guessed {guess} in room {_currentRoomId}.");
            IRoomGrain roomGrain = GrainFactory.GetGrain<IRoomGrain>(_currentRoomId);
            return await roomGrain.ProcessGuess(this.GetPrimaryKey(), guess);
        }

        public Task<int> GetPoints()
        {
            return Task.FromResult(State.Score);
        }

        public async Task AddPoint()
        {
            State.Score++;
            await WriteStateAsync(); // Persist the state
        }

        public Task LeaveRoom()
        {
            _currentRoomId = Guid.Empty;
            Console.WriteLine($"Player {this.GetPrimaryKey()} left the room.");
            return Task.CompletedTask;
        }

        public Task<Guid> GetRoomId()
        {
            return Task.FromResult(_currentRoomId);
        }
    }

    public interface IRoomGrain : IGrainWithGuidKey
    {
        Task<bool> StartGame();
        Task<int> ProcessGuess(Guid playerId, int guess);
        Task<int> GetResult(Guid playerId);
        Task<Dictionary<Guid, int>> GetCurrentRoundGuesses();
        Task AddPlayer(Guid playerId);
    }

    public class RoomGrain : Grain, IRoomGrain
    {
        private readonly List<Guid> _players = new List<Guid>();
        private int _targetNumber;
        private Dictionary<Guid, int> _currentRoundGuesses = new Dictionary<Guid, int>();
        private Guid _winnerId;

        public Task<bool> StartGame()
        {
            if (_players.Count != 2)
            {
                Console.WriteLine($"Seems like there's been an error when subscribing players to that game.");
                return Task.FromResult(false);
            }

            Console.WriteLine($"Starting game in room {this.GetPrimaryKey()}.");
            GenerateTargetNumber();
            return Task.FromResult(true);
        }

        public Task<int> ProcessGuess(Guid playerId, int guess)
        {
            _currentRoundGuesses[playerId] = guess;
            Console.WriteLine($"Player {playerId} guessed {guess} in room {this.GetPrimaryKey()}.");

            if (_currentRoundGuesses.Count == _players.Count)
            {
                _winnerId = GetWinner();
                GrainFactory.GetGrain<IPlayerGrain>(_winnerId).AddPoint();
                return Task.FromResult(RelativeResult(playerId));
            }
            return Task.FromResult(0);
        }

        public Task<Dictionary<Guid, int>> GetCurrentRoundGuesses()
        {
            return Task.FromResult(_currentRoundGuesses);
        }

        private Guid GetWinner()
        {
            if (_currentRoundGuesses.First().Value == _currentRoundGuesses.Last().Value)
                return Guid.Empty;

            Guid winner = _currentRoundGuesses
                .OrderBy(x => Math.Abs(x.Value - _targetNumber))
                .FirstOrDefault().Key;

            return winner;
        }

        public Task<int> GetResult(Guid playerId)
        {
            return Task.FromResult(RelativeResult(playerId));
        }

        private int RelativeResult(Guid playerId)
        {
            if (_currentRoundGuesses.Count == _players.Count)
            {
                if (_winnerId == Guid.Empty)
                {
                    return 3;
                }
                return (_winnerId == playerId) ? 1 : 2;
            }
            else
            {
                return 0;
            }
        }

        public Task AddPlayer(Guid playerId)
        {
            _players.Add(playerId);
            Console.WriteLine($"Added player {playerId} to room {this.GetPrimaryKey()}.");
            return Task.CompletedTask;
        }

        private void GenerateTargetNumber()
        {
            Random random = new Random();
            _targetNumber = random.Next(0, 101);
            Console.WriteLine($"Target number in room {this.GetPrimaryKey()}: {_targetNumber}");
        }
    }
}