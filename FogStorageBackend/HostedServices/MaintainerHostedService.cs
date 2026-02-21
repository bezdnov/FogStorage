using Microsoft.Extensions.Hosting;

namespace FogStorageBackend.HostedServices;

public class MaintainerHostedService: IHostedService
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