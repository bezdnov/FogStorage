using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FogStorageBackend.HostedServices;

/*
 * CheckerHostedService
 * Task of this hosted service is to check availability of a file every 
 */
public class CheckerHostedService: IHostedService
{
    private ILogger _logger;
    private IShardOperator _shardOperator;
    private IFileOperator _fileOperator;
    
    public CheckerHostedService(ILogger logger, IShardOperator shardOperator, IFileOperator fileOperator)
    {
        _logger = logger;
        _shardOperator = shardOperator;
        _fileOperator = fileOperator;
    }
    
    // Sends requests to the network to check:
    // amount of nodes which store each shard for 1 file
    private void CheckFileAvailability()
    {
        
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}