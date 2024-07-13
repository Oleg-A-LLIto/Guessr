using Guessr;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Threading.Tasks;

namespace OrleansGuessr
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("Silo started. Press any key to terminate...");
                Console.ReadKey();

                await host.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
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
                            options.ServiceId = "OrleansHelloWorld";
                        })
                        .ConfigureLogging(logging => logging.AddConsole());
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services => // Add this block
                    {
                        services.AddCors(options =>
                        {
                            options.AddDefaultPolicy(builder =>
                            {
                                builder.AllowAnyOrigin()
                                       .AllowAnyMethod() 
                                       .AllowAnyHeader(); 
                            });
                        });
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseCors(); 
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/hello/{name}", async context =>
                            {
                                var grainFactory = context.RequestServices.GetService<IGrainFactory>();
                                var helloWorldGrain = grainFactory.GetGrain<IGuessrGrain>(Guid.NewGuid());
                                var result = await helloWorldGrain.SayHello(context.Request.RouteValues["name"]?.ToString() ?? "World");
                                await context.Response.WriteAsync(result);
                            });
                        });
                    });
                });

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}