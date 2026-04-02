using FogStorageBackend.Constants;
using FogStorageBackend.Repository;
using FogStorageBackend.WebHandling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FogStorageBackend.HostedServices;

/*
 * KeeperHostedService
 * Keeps the information about other users' shards
 * deletes shards if they're not updated by other peers
 */
public class KeeperHostedService(
    ILogger<KeeperHostedService> logger,
    IDbRepository repository,
    IShardOperator shardOperator)
    : BackgroundService
{
    private IDbRepository _repository = repository;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000 * StorageConstants.FileStorageTimeout, stoppingToken);
            
            logger.LogInformation("Keeper Hosted Service searches for forgotten shards");
            foreach (var shard in shardOperator.LoadAllShards())
            {
                if (shard.ShardLastCheckTime < DateTime.UtcNow + TimeSpan.FromSeconds(StorageConstants.FileStorageTimeout))
                {
                    logger.LogDebug("(not) deleting file from file system. File public key: {pubkey}", shard.FilePublicKey);
                }
                else
                {
                    logger.LogDebug("100% not deleting file from file system. File public key: {pubkey}", shard.FilePublicKey);
                }
            }
        }
    }
}