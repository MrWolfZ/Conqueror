using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal interface ICommandMiddlewareInvoker
{
    Type MiddlewareType { get; }

    Task<TResponse> Invoke<TCommand, TResponse>(TCommand command,
                                                CommandMiddlewareNext<TCommand, TResponse> next,
                                                object? middlewareConfiguration,
                                                IServiceProvider serviceProvider,
                                                CancellationToken cancellationToken)
        where TCommand : class;
}
