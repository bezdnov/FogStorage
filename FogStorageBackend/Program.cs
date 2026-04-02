// See https://aka.ms/new-console-template for more information

using System.Text;
using FogStorageBackend.Configuration;
using FogStorageBackend.Constants;
using FogStorageBackend.HostedServices;
using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using FogStorageBackend.WebHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class Program
{
    public static async Task Main(string[] args)
    {
        /*
        ApplicationGeneralSettings appSettings = new ApplicationGeneralSettings()
        {
            ApplicationDefaultFolder = "/home/cursed_nerd/.local/share/FogStorage/",
            ShardFolderName = "Shards",
            DownloadFolder = "/home/cursed_nerd/Downloads/",
            DbFolderName = "Db"
        };
        
        var factory = new LoggerFactory();
        ILogger<ShardOperator> logger1 = new Logger<ShardOperator>(factory);
        ILogger<WebSocketsCommunicator> logger2 = new Logger<WebSocketsCommunicator>(factory);
        ILogger<FileOperator> logger3 = new Logger<FileOperator>(factory);
        
        ShardOperator so = new ShardOperator(logger1, appSettings);
        FileOperator fo = new FileOperator(logger3, appSettings);
        var communicator = new WebSocketsCommunicator(logger2, so, fo);
        await communicator.Init();
        
        List<string> names = so.GetShardNames();
        
        if (so.LoadShardByName(names[0]) is { } shard)
        {
            await communicator.SendShard(shard);
        }
        */

        var host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
        {
            services.Configure<ApplicationGeneralSettings>(context.Configuration.GetSection("ApplicationGeneralSettings"));

            // services.Configure<SimulationOptions>(context.Configuration.GetSection("Simulation"));

            services.AddSingleton<IShardOperator, ShardOperator>();
            services.AddSingleton<IFileOperator, FileOperator>();
            services.AddSingleton<IDbRepository, DbRepository>();
            services.AddSingleton<WebSocketsCommunicator>();

            services.AddHostedService<InitializerHostedService>();
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
