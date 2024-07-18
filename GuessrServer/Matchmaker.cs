using Guessr.Grains;

namespace Guessr
{
    public interface IMatchmakingService
    {
        void EnqueuePlayer(Guid playerId);
    }

    public class MatchmakingService : IMatchmakingService
    {
        private readonly IGrainFactory _grainFactory;
        private readonly Queue<Guid> _playerQueue = new Queue<Guid>();

        public MatchmakingService(ILogger<MatchmakingService> logger, IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public async void EnqueuePlayer(Guid playerId)
        {
            Console.WriteLine($"Player {playerId} enqueued for matchmaking.");

            if (_playerQueue.Count > 0)
            {
                Guid player1 = _playerQueue.Dequeue();
                Guid player2 = playerId;
                _ = CreateRoom(player1, player2);
            }
            else
            {
                _playerQueue.Enqueue(playerId);
            }
        }

        private async Task CreateRoom(Guid player1, Guid player2)
        {
            Guid roomId = Guid.NewGuid();
            IRoomGrain roomGrain = _grainFactory.GetGrain<IRoomGrain>(roomId);

            await roomGrain.AddPlayer(player1);
            await roomGrain.AddPlayer(player2);

            await _grainFactory.GetGrain<IPlayerGrain>(player1).JoinRoom(roomId);
            await _grainFactory.GetGrain<IPlayerGrain>(player2).JoinRoom(roomId);

            Console.WriteLine($"Created room {roomId} for players with GUIDs: [{player1}] and [{player2}].");

            _ = roomGrain.StartGame();
        }
    }
}