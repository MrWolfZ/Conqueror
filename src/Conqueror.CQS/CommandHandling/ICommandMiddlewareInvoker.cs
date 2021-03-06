using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling
{
    internal delegate Task<TResponse> CommandMiddlewareNext<in TCommand, TResponse>(TCommand command, CancellationToken cancellationToken);

    internal interface ICommandMiddlewareInvoker
    {
        Task<TResponse> Invoke<TCommand, TResponse>(TCommand command,
                                                    CommandMiddlewareNext<TCommand, TResponse> next,
                                                    Type middlewareType,
                                                    object? middlewareConfiguration,
                                                    IServiceProvider serviceProvider,
                                                    CancellationToken cancellationToken)
            where TCommand : class;
    }
}
