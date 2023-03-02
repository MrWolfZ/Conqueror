using System;
using System.Collections.Generic;

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

    /// <inheritdoc />
    public IDictionary<string, string> Items => wrappedContext.Items;

    /// <inheritdoc />
    public bool HasItems => wrappedContext.HasItems;

    /// <inheritdoc />
    public string TraceId => wrappedContext.TraceId;

    /// <inheritdoc />
    public void SetTraceId(string traceId) => wrappedContext.SetTraceId(traceId);

    public void Dispose() => onDispose?.Invoke();
}
