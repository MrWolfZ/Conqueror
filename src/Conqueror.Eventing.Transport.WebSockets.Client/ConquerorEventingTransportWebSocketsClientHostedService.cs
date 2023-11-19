using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Conqueror.Eventing.Transport.WebSockets.Client;

internal sealed class ConquerorEventingTransportWebSocketsClientHostedService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO
        // 1. find all event observer registrations
        // 2. determine all websockets event types
        // 3. find and group event types by base address
        // 4. for each base address, create a web socket client and run it until cancellation

        return Task.CompletedTask;
    }
}
