using System;

namespace Conqueror.Common;

internal sealed class DisposableConquerorContext(
    IConquerorContext wrappedContext,
    Action? onDispose = null)
    : IDisposableConquerorContext
{
    public string TraceId => wrappedContext.TraceId;

    public IConquerorContextData DownstreamContextData => wrappedContext.DownstreamContextData;

    public IConquerorContextData UpstreamContextData => wrappedContext.UpstreamContextData;

    public IConquerorContextData ContextData => wrappedContext.ContextData;

    public void SetTraceId(string traceId) => wrappedContext.SetTraceId(traceId);

    public void Dispose() => onDispose?.Invoke();
}
