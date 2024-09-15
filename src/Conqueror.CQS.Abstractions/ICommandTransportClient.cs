using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface ICommandTransportClient
{
    string TransportTypeName { get; }

    Task<TResponse> Send<TCommand, TResponse>(TCommand command,
                                              IServiceProvider serviceProvider,
                                              CancellationToken cancellationToken)
        where TCommand : class;
}
