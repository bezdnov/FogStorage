using Avalonia;
using System;
using FogStorageBackend.Configuration;
using FogStorageBackend.HostedServices;
using FogStorageBackend.Repository;
using FogStorageBackend.WebHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FogStorageGUI;

class Program
{
    public static IHost AppHost;
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.Configure<ApplicationGeneralSettings>(context.Configuration.GetSection("ApplicationGeneralSettings"));

                services.AddSingleton<IFileOperator, FileOperator>();
                services.AddSingleton<IShardOperator, ShardOperator>();
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
        
        AppHost.RunAsync();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
