// See https://aka.ms/new-console-template for more information

using System.Text;
using FogStorageBackend.Configuration;
using FogStorageBackend.HostedServices;
using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using FogStorageBackend.REST;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class Program
{
    public static void Main(string[] args)
    {
        
        var host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
        {
            services.Configure<ApplicationGeneralSettings>(context.Configuration.GetSection("ApplicationGeneralSettings"));

            // services.Configure<SimulationOptions>(context.Configuration.GetSection("Simulation"));

            services.AddSingleton<IShardOperator, ShardOperator>();
            services.AddSingleton<IFileOperator, FileOperator>();

            services.AddHostedService<CheckerHostedService>();
            services.AddHostedService<KeeperHostedService>();
        }).ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .Build();
        host.Run();
    }
}
