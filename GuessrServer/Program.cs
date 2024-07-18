using Orleans.Configuration;

namespace Guessr
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();

                IHost host = await StartSilo(configuration);

                Console.WriteLine("Silo started. Press any key to terminate...");
                Console.ReadKey();
                await host.StopAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<IHost> StartSilo(IConfiguration configuration)
        {
            IHostBuilder builder = new HostBuilder()
                .UseOrleans(silo =>
                {
                    var connectionString = configuration.GetConnectionString("OrleansDb");

                    silo
                        .UseLocalhostClustering()
                        .AddAdoNetGrainStorage("PlayerStore", options =>
                        {
                            options.Invariant = "System.Data.SqlClient";
                            options.ConnectionString = connectionString;
                        })
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "Guessr";
                        });
                }).ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddCors(options => // otherwise the web client just cries about the CORS
                        {
                            options.AddDefaultPolicy(builder =>
                            {
                                builder
                                    .AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader();
                            });
                        });
                        services.AddControllers();
                        services.AddSingleton<IMatchmakingService, MatchmakingService>();
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseCors();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                });

            IHost host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}