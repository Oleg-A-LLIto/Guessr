using Orleans.Configuration;

namespace Guessr
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                IHost host = await StartSilo();
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

        private static async Task<IHost> StartSilo()
        {
            var builder = new HostBuilder()
            .UseOrleans(silo =>
                {
                    silo
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "Guessr";
                        });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddCors(options =>
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
                    webBuilder.Configure(app =>     // otherwise the web client just cries about the CORS
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
