using System;

namespace Conqueror.Common;

internal sealed class DisposableConquerorContext : IDisposableConquerorContext
{
    private readonly Action? onDispose;
    private readonly IConquerorContext wrappedContext;

    public DisposableConquerorContext(IConquerorContext wrappedContext, Action? onDispose = null)
    {
        this.onDispose = onDispose;
        this.wrappedContext = wrappedContext;
    }

    public string TraceId => wrappedContext.TraceId;

    public IConquerorContextData DownstreamContextData => wrappedContext.DownstreamContextData;

    public IConquerorContextData UpstreamContextData => wrappedContext.UpstreamContextData;

    public IConquerorContextData ContextData => wrappedContext.ContextData;

    public void SetTraceId(string traceId) => wrappedContext.SetTraceId(traceId);

    public void Dispose() => onDispose?.Invoke();
}
