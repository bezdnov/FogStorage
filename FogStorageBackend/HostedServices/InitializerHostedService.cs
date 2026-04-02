using FogStorageBackend.Configuration;
using FogStorageBackend.WebHandling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FogStorageBackend.HostedServices;

/*
 * This hosted service exists for one reason: there's a need in initilization of WebSocketsCommunicator,
 * folders and database
 */
public class InitializerHostedService(ILogger<InitializerHostedService> logger, WebSocketsCommunicator communicator, IOptions<ApplicationGeneralSettings> applicationSettings)
    : IHostedService
{
    private readonly ILogger<InitializerHostedService> _logger = logger;
    private readonly ApplicationGeneralSettings _appSettings = applicationSettings.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initialization of communicator");
        await communicator.Init();
        
        _logger.LogDebug("Initialization of FogStorage default folders");
        
        if (!Directory.Exists(_appSettings.ApplicationDefaultFolder))
            Directory.CreateDirectory(_appSettings.ApplicationDefaultFolder);
        
        var shardFolder = Path.Combine(_appSettings.ApplicationDefaultFolder, _appSettings.ShardFolderName);
        var dbFolder = Path.Combine(_appSettings.ApplicationDefaultFolder, _appSettings.DbFolderName);
        
        if (!Directory.Exists(shardFolder))
            Directory.CreateDirectory(shardFolder);
        
        if (!Directory.Exists(dbFolder))
            Directory.CreateDirectory(dbFolder);
        
        

        _logger.LogDebug("Initialization ended");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}