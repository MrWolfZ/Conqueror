using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandMiddlewareRegistry
{
    private readonly IReadOnlyDictionary<Type, ICommandMiddlewareInvoker> invokers;

    public CommandMiddlewareRegistry(IEnumerable<ICommandMiddlewareInvoker> invokers)
    {
        this.invokers = invokers.ToDictionary(i => i.MiddlewareType);
    }

    public ICommandMiddlewareInvoker? GetCommandMiddlewareInvoker<TMiddleware>()
        where TMiddleware : ICommandMiddlewareMarker
    {
        return invokers.GetValueOrDefault(typeof(TMiddleware));
    }
}
