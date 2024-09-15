using System;

namespace Conqueror.Common;

internal sealed class DisposableConquerorContext(
    IConquerorContext wrappedContext,
    Action? onDispose = null)
    : IDisposableConquerorContext
{
    public IConquerorContextData DownstreamContextData => wrappedContext.DownstreamContextData;

    public IConquerorContextData UpstreamContextData => wrappedContext.UpstreamContextData;

    public IConquerorContextData ContextData => wrappedContext.ContextData;

    public void Dispose() => onDispose?.Invoke();
}
