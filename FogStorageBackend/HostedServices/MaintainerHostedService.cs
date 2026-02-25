using FogStorageBackend.Model;
using FogStorageBackend.Repository;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FogStorageBackend.HostedServices;

/*
 * Role of a maintainer is in sending requests to handle user requests (mode from GUI) and transmitting them to network
 * a local HTTP server.
 */
public class MaintainerHostedService: IHostedService
{
    private ILogger _logger;
    private IShardOperator _shardOperator;
    private IFileOperator _fileOperator;
    
    public MaintainerHostedService(ILogger logger, IShardOperator shardOperator, IFileOperator fileOperator)
    {
        _logger = logger;
        _shardOperator = shardOperator;
        _fileOperator = fileOperator;
    }

    // 
    private void StoreFile(string filePath)
    {
        var file = _fileOperator.ReadFile(filePath);
        Shard[] shards = _shardOperator.SplitFile(file);
        
        // send a request to network containing all the needed information
    }

    // send a request to delete file from network
    private void DeleteFile(string filePrivateKey)
    {
        // send a request...
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