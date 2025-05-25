using System;

namespace Conqueror.Context;

internal sealed class DefaultConquerorContext : ConquerorContext
{
    private readonly Action<DefaultConquerorContext> onDispose;
    private readonly DefaultConquerorContext? parent;

    private DefaultConquerorContext(Action<ConquerorContext> onDispose)
    {
        this.onDispose = onDispose;
        parent = null;
        DownstreamContextData = new();
        UpstreamContextData = new();
        ContextData = new();
    }

    private DefaultConquerorContext(DefaultConquerorContext parent, Action<DefaultConquerorContext> onDispose)
    {
        this.onDispose = onDispose;
        this.parent = parent;

        DownstreamContextData = new(parent.DownstreamContextData);
        ContextData = new(parent.ContextData);

        // Upstream data is initially empty since it flows up from child to parent
        UpstreamContextData = new();
    }

    public override DefaultConquerorContextData DownstreamContextData { get; }

    public override DefaultConquerorContextData UpstreamContextData { get; }

    public override DefaultConquerorContextData ContextData { get; }

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
        if (!childContext.UpstreamContextData.IsEmpty)
        {
            foreach (var (key, value, scope) in childContext.UpstreamContextData)
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
        if (!ContextData.IsEmpty)
        {
            // bidirectional keys also propagate deletion upstream
            foreach (var (key, _, _) in ContextData)
            {
                if (childContext.ContextData.IsRemoved(key))
                {
                    _ = ContextData.Remove(key);
                }
            }
        }

        // performance optimization to prevent unnecessary allocation of enumerator
        if (!childContext.ContextData.IsEmpty)
        {
            foreach (var (key, value, scope) in childContext.ContextData)
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
