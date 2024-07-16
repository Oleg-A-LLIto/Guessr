using Microsoft.AspNetCore.Mvc;
using Orleans;
using System;
using System.Threading.Tasks;
using Guessr;
using Guessr.Grains;

namespace Guessr.Controllers
{
    [ApiController]
    [Route("api/game")]
    public class GameController : ControllerBase
    {
        private readonly IClusterClient _client;
        private readonly IMatchmakingService _matchmakingService;

        public GameController(IClusterClient client, IMatchmakingService matchmakingService)
        {
            _client = client;
            _matchmakingService = matchmakingService;
        }

        [HttpGet("joinQueue")]
        public async Task<IActionResult> JoinQueue(Guid playerId)
        {
            IPlayerGrain playerGrain = _client.GetGrain<IPlayerGrain>(playerId);
            await playerGrain.JoinQueue();
            _matchmakingService.EnqueuePlayer(playerId);
            return Ok(new { message = $"Successfully joined the queue.", playerId = playerId   });
        }

        [HttpGet("initPlayer")]
        public async Task<IActionResult> initPlayer()
        {
            Guid playerId = Guid.NewGuid();
            return Ok(new { message = $"Hello, new player!", playerId = playerId });
        }

        [HttpGet("getRoomStatus")]
        public async Task<IActionResult> GetRoomStatus(Guid playerId)
        {
            Console.WriteLine($"API: Checking room status for Player {playerId}");
            var playerGrain = _client.GetGrain<IPlayerGrain>(playerId);
            var currentRoomId = await playerGrain.GetRoomId();

            if (currentRoomId != Guid.Empty)
            {
                return Ok(new { joinedRoom = true, roomId = currentRoomId });
            }
            else
            {
                return Ok(new { joinedRoom = false });
            }
        }

        [HttpGet("makeGuess")]
        public async Task<IActionResult> MakeGuess(Guid playerId, int guess)
        {
            Console.WriteLine($"API: Player {playerId} attempting to make a guess: {guess}");
            IPlayerGrain playerGrain = _client.GetGrain<IPlayerGrain>(playerId);
            int result = await playerGrain.MakeGuess(guess);
            return Ok(new { message = $"Your guess is {guess}.", gameResult = result });
        }

        [HttpGet("pollGameStatus")]
        public async Task<IActionResult> PollGameStatus(Guid playerId)
        {
            IPlayerGrain playerGrain = _client.GetGrain<IPlayerGrain>(playerId);
            int result = await playerGrain.GetResult();
            return Ok(new { gameResult = result });
        }

        [HttpGet("getScore")]
        public async Task<IActionResult> GetPlayerScore(Guid playerId)
        {
            var playerGrain = _client.GetGrain<IPlayerGrain>(playerId);
            int score = await playerGrain.GetPoints();
            return Ok(new { score = score });
        }
    }
}