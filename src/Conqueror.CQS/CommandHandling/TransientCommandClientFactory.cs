using System;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class TransientCommandClientFactory(
    CommandClientFactory innerFactory,
    IServiceProvider serviceProvider)
    : ICommandClientFactory
{
    public THandler CreateCommandClient<THandler>(Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory)
        where THandler : class, ICommandHandler
    {
        return innerFactory.CreateCommandClient<THandler>(serviceProvider, transportClientFactory);
    }
}
