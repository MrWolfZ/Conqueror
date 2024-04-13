using System;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandTransportClientFactory
{
    private readonly ICommandTransportClient? transportClient;
    private readonly Func<ICommandTransportClientBuilder, ICommandTransportClient>? syncTransportClientFactory;
    private readonly Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>>? asyncTransportClientFactory;

    public CommandTransportClientFactory(ICommandTransportClient transportClient)
    {
        this.transportClient = transportClient;
    }

    public CommandTransportClientFactory(Func<ICommandTransportClientBuilder, ICommandTransportClient>? syncTransportClientFactory)
    {
        this.syncTransportClientFactory = syncTransportClientFactory;
    }

    public CommandTransportClientFactory(Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>>? asyncTransportClientFactory)
    {
        this.asyncTransportClientFactory = asyncTransportClientFactory;
    }

    public Task<ICommandTransportClient> Create(Type commandType, IServiceProvider serviceProvider)
    {
        if (transportClient is not null)
        {
            return Task.FromResult(transportClient);
        }

        var transportBuilder = new CommandTransportClientBuilder(serviceProvider, commandType);

        if (syncTransportClientFactory is not null)
        {
            return Task.FromResult(syncTransportClientFactory.Invoke(transportBuilder));
        }

        if (asyncTransportClientFactory is not null)
        {
            return asyncTransportClientFactory.Invoke(transportBuilder);
        }

        // this code should not be reachable
        throw new InvalidOperationException($"could not create transport client for command type '{commandType.Name}' since it was not configured with a factory");
    }
}
