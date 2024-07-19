using Guessr.Grains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;

namespace Guessr.Client
{
    class Program
    {
        static Guid _playerId;
        static IGrainFactory _client;

        static async Task Main(string[] args)
        {
            while (true)
            {
                try
                {
                    using IHost host = Host.CreateDefaultBuilder(args)
                    .UseOrleansClient((context, client) =>
                    {
                        client.Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "Guessr";
                        })
                        .UseLocalhostClustering();
                    })
                    .UseConsoleLifetime()
                    .Build();

                    await host.StartAsync();

                    _client = host.Services.GetRequiredService<IGrainFactory>();

                    Console.WriteLine("Connected to server!\n");

                    LoadPlayerId();
                    await GameLoop();

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to the server: {ex.Message}");
                    Console.WriteLine("Please check your connection and try again.");
                    Console.WriteLine("1. Retry");
                    Console.WriteLine("2. Exit");

                    var choice = Console.ReadLine();

                    if (choice != "1")
                    {
                        return;
                    }
                }
            }
        }

        static async Task GameLoop()
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("1. Join Queue");
                Console.WriteLine("2. Exit");
                Console.Write("Choose an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await JoinQueue();
                        break;
                    case "2":
                        return;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }

        static void LoadPlayerId()
        {
            if (File.Exists("playerid.txt"))
            {
                _playerId = Guid.Parse(File.ReadAllText("playerid.txt"));
                Console.WriteLine($"Welcome back, player {_playerId}!");
                DisplayScore().Wait();
            }
            else
            {
                _playerId = Guid.NewGuid();
                File.WriteAllText("playerid.txt", _playerId.ToString());
                Console.WriteLine($"Welcome, new player! Your ID is {_playerId}. You have 0 points.");
            }
        }

        static async Task JoinQueue()
        {
            IPlayerGrain playerGrain = _client.GetGrain<IPlayerGrain>(_playerId);
            await playerGrain.JoinQueue();
            Console.WriteLine("Joined the queue. Waiting for another player...");

            bool roomJoined = false;
            Guid roomId = Guid.Empty;

            while (!roomJoined)
            {
                await Task.Delay(1000);
                roomId = await playerGrain.GetRoomId();
                if (roomId != Guid.Empty)
                {
                    roomJoined = true;
                }
            }

            Console.WriteLine($"Joined room {roomId}.");
            await PlayGame(roomId);
        }

        static async Task PlayGame(Guid roomId)
        {
            IPlayerGrain playerGrain = _client.GetGrain<IPlayerGrain>(_playerId);
            int guess;
            do
            {
                Console.Write("Enter your guess (0-100): ");
            } while (!int.TryParse(Console.ReadLine(), out guess) || guess < 0 || guess > 100);

            int result = await playerGrain.MakeGuess(guess);

            while (result == 0)
            {
                await Task.Delay(1000);
                result = await playerGrain.GetResult();
            }

            switch (result)
            {
                case 1:
                    Console.WriteLine("You win!");
                    break;
                case 2:
                    Console.WriteLine("You lose.");
                    break;
                case 3:
                    Console.WriteLine("It's a draw!");
                    break;
            }

            await DisplayScore();
            await playerGrain.LeaveRoom();
        }

        static async Task DisplayScore()
        {
            IPlayerGrain playerGrain = _client.GetGrain<IPlayerGrain>(_playerId);
            int score = await playerGrain.GetPoints();
            Console.WriteLine($"Your score: {score}");
        }
    }
}