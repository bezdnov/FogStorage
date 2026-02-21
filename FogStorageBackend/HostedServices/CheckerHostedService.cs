using Microsoft.Extensions.Hosting;

namespace FogStorageBackend.HostedServices;

/*
 * CheckerHostedService
 * Task of this hosted service is to check availability of a file every 
 */
public class CheckerHostedService: IHostedService
{
    public CheckerHostedService()
    {
        
    }
    
    void CheckFileAvailability()
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