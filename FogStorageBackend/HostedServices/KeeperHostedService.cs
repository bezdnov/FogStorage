using Microsoft.Extensions.Hosting;

namespace FogStorageBackend.HostedServices;

/*
 * KeeperHostedService
 * Keeps the information about other users' shards
 * 
 */
public class KeeperHostedService: IHostedService
{
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