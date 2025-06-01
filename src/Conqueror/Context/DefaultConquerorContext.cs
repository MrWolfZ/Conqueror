using System;
using System.Threading;

namespace Conqueror.Context;

internal sealed class DefaultConquerorContext : ConquerorContext
{
    private readonly Action<DefaultConquerorContext> onDispose;
    private readonly DefaultConquerorContext? parent;

    private DefaultConquerorContextData? downstreamContextData;
    private DefaultConquerorContextData? upstreamContextData;
    private DefaultConquerorContextData? contextData;

    private DefaultConquerorContext(Action<ConquerorContext> onDispose)
    {
        this.onDispose = onDispose;
        parent = null;
    }

    private DefaultConquerorContext(DefaultConquerorContext parent, Action<DefaultConquerorContext> onDispose)
    {
        this.onDispose = onDispose;
        this.parent = parent;

        downstreamContextData = parent.downstreamContextData is not null ? new(parent.downstreamContextData) : null;
        contextData = parent.contextData is not null ? new(parent.contextData) : null;
    }

    public override DefaultConquerorContextData DownstreamContextData
        => LazyInitializer.EnsureInitialized(ref downstreamContextData, static () => new());

    public override DefaultConquerorContextData UpstreamContextData
        => LazyInitializer.EnsureInitialized(ref upstreamContextData, static () => new());

    public override DefaultConquerorContextData ContextData
        => LazyInitializer.EnsureInitialized(ref contextData, static () => new());

    public static DefaultConquerorContext CreateRootContext(Action<ConquerorContext> onRootDispose)
    {
        return new(onRootDispose);
    }

    public DefaultConquerorContext CreateChildContext(Action onChildDispose)
    {
        return new(
            this,
            ctx =>
            {
                PropagateUpstreamData(ctx);
                onChildDispose();
            });
    }

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            parent?.PropagateUpstreamData(this);

            onDispose(this);
        }
    }

    private void PropagateUpstreamData(DefaultConquerorContext childContext)
    {
        // performance optimization to prevent unnecessary allocation of enumerator
        if (childContext.upstreamContextData is not null && !childContext.upstreamContextData.IsEmpty)
        {
            foreach (var (key, value, scope) in childContext.upstreamContextData)
            {
                if (value is string s)
                {
                    UpstreamContextData.Set(key, s, scope);
                }
                else
                {
                    UpstreamContextData.Set(key, value);
                }
            }
        }

        // performance optimization to prevent unnecessary allocation of enumerator
        if (contextData is not null && !contextData.IsEmpty)
        {
            // bidirectional keys also propagate deletion upstream
            foreach (var (key, _, _) in contextData)
            {
                if (childContext.contextData?.IsRemoved(key) ?? false)
                {
                    _ = contextData.Remove(key);
                }
            }
        }

        // performance optimization to prevent unnecessary allocation of enumerator
        if (childContext.contextData is not null && !childContext.contextData.IsEmpty)
        {
            foreach (var (key, value, scope) in childContext.contextData)
            {
                if (value is string s)
                {
                    ContextData.Set(key, s, scope);
                }
                else
                {
                    ContextData.Set(key, value);
                }
            }
        }
    }
}
