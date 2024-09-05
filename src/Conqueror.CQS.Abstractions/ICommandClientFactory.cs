using System;
using System.Threading.Tasks;

namespace Conqueror;

public interface ICommandClientFactory
{
    THandler CreateCommandClient<THandler>(Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory)
        where THandler : class, ICommandHandler
    {
        return CreateCommandClient<THandler>(b => Task.FromResult(transportClientFactory(b)));
    }

    THandler CreateCommandClient<THandler>(Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory)
        where THandler : class, ICommandHandler;
}
