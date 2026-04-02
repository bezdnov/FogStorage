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
public class CheckerHostedService: BackgroundService
{
    private ILogger<CheckerHostedService> _logger;
    private IShardOperator _shardOperator;
    private IFileOperator _fileOperator;
    private WebSocketsCommunicator _webSocketsCommunicator;
    private ApplicationGeneralSettings _settings;
    private IDbRepository _dbRepository;
    
    public CheckerHostedService (
        ILogger<CheckerHostedService> logger, 
        IShardOperator shardOperator, 
        IFileOperator fileOperator,  
        WebSocketsCommunicator webSocketsCommunicator,
        IDbRepository dbRepository
        )
    {
        _logger = logger;
        _shardOperator = shardOperator;
        _fileOperator = fileOperator;
        _webSocketsCommunicator = webSocketsCommunicator;
        _dbRepository = dbRepository;
    }
    
    // Sends requests to the network to check:
    // auto-deletes files unchecked by Rightholder
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(StorageConstants.CheckTimeout * 1000);
            _logger.LogInformation("Checking shard availability");
            
            var publicKeys = _dbRepository.GetPublicKeys();
            var tasks = publicKeys.Select(publicKey => _webSocketsCommunicator.CheckFileStatus(publicKey)).ToList();
            await Task.WhenAll(tasks);
            _logger.LogInformation("Checking shards completed; waiting some time...");
        }
    }
}