using FogStorageBackend.WebHandling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FogStorageBackend.HostedServices;

/*
 * KeeperHostedService
 * Keeps the information about other users' shards
 * 
 */
public class KeeperHostedService: IHostedService
{
    public KeeperHostedService(ILogger<KeeperHostedService> logger, WebSocketsCommunicator communicator)
    {
        
    } 
    
    private void CheckShard()
    {
        
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        while (true) ;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        while (true) ;
    }
}