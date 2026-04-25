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
 * Task of this hosted service is to check availability of a file every 60 seconds
 */
public class CheckerHostedService(
    ILogger<CheckerHostedService> logger,
    WebSocketsCommunicator webSocketsCommunicator,
    IDbRepository dbRepository)
    : BackgroundService
{
    // Sends requests to the network to check:
    // auto-deletes files unchecked by Rightholder
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(StorageConstants.CheckTimeout * 1000, stoppingToken);
            logger.LogInformation("Checking shard availability");
            
            var publicKeys = dbRepository.GetPublicKeys();
            var tasks = publicKeys.Select(webSocketsCommunicator.CheckFileStatus).ToList();
            await Task.WhenAll(tasks);
            foreach (var task in tasks) {
                logger.LogInformation($"Shards and their availability: {string.Join(' ', task.Result)}");
            }
            logger.LogInformation("Checking shards completed; waiting some time...");
        }
    }
}