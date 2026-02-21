using Microsoft.Extensions.Hosting;

namespace FogStorageBackend.HostedServices;

/*
 * KeeperHostedService
 * Keeps the information about other users' shards
 * 
 */
public class KeeperHostedService: IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}