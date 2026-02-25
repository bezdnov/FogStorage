// See https://aka.ms/new-console-template for more information

using FogStorageBackend.Configuration;
using FogStorageBackend.HostedServices;
using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class Program
{
    public static void Main(string[] args)
    {
        /*
        ShardOperator so = new ShardOperator();
        FileOperator fileOp = new FileOperator();
        StoredFileInfo fi = fileOp.ReadFile("/home/cursed/test/testfile");
        Shard[] shards = so.SplitFile(fi);
        
        foreach (byte b in shards[0].ShardBytes)
        {
            Console.Write(char.ConvertFromUtf32(b));
        }
        
        foreach (byte b in shards[1].ShardBytes)
        {
            Console.Write(char.ConvertFromUtf32(b));
        }
        
        */
        
        
        
        var host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
        {
            services.Configure<ApplicationGeneralSettings>(context.Configuration.GetSection("ApplicationGeneralSettings"));
            services.Configure<StorageConfiguration>(context.Configuration.GetSection("StorageConfiguration"));
            
            // services.Configure<SimulationOptions>(context.Configuration.GetSection("Simulation"));
            
            services.AddSingleton<IShardOperator, ShardOperator>();
            services.AddSingleton<IFileOperator, FileOperator>();

            services.AddHostedService<CheckerHostedService>();
            services.AddHostedService<KeeperHostedService>();
            services.AddHostedService<MaintainerHostedService>();
            
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
