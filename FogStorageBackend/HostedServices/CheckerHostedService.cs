using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FogStorageBackend.REST;

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
    private WebHandler _webHandler;
    
    public CheckerHostedService(ILogger<CheckerHostedService> logger, IShardOperator shardOperator, IFileOperator fileOperator)
    {
        _logger = logger;
        _shardOperator = shardOperator;
        _fileOperator = fileOperator;
        _webHandler = new WebHandler();
    }
    
    // Sends requests to the network to check:
    // amount of nodes which store each shard for 1 file
    private void CheckFileAvailability()
    {
        
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}