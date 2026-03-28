using FogStorageBackend.Configuration;
using FogStorageBackend.Constants;
using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FogStorageBackend.WebHandling;

namespace FogStorageBackend.HostedServices;

/*
 * CheckerHostedService
 * Task of this hosted service is to check availability of a file every 
 */
public class CheckerHostedService: IHostedService
{
    private ILogger<CheckerHostedService> _logger;
    private IShardOperator _shardOperator;
    private IFileOperator _fileOperator;
    private WebSocketsCommunicator _webSocketsCommunicator;
    private ApplicationGeneralSettings _settings;
    
    public CheckerHostedService(
        ILogger<CheckerHostedService> logger, 
        IShardOperator shardOperator, 
        IFileOperator fileOperator,  
        WebSocketsCommunicator webSocketsCommunicator
        )
    {
        _logger = logger;
        _shardOperator = shardOperator;
        _fileOperator = fileOperator;
        _webSocketsCommunicator = webSocketsCommunicator;
    }
    
    // Sends requests to the network to check:
    // auto-deletes files unchecked by Rightholder
    public Task StartAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task.Delay(StorageConstants.CheckTimeout);
            
            // TODO getting private key from local storage
            _webSocketsCommunicator.CheckFileStatus("private key");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}