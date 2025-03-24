using System;

namespace Conqueror.Context;

internal sealed class DefaultConquerorContext(Action<ConquerorContext> onDispose) : ConquerorContext
{
    public override IConquerorContextData DownstreamContextData { get; } = new DefaultConquerorContextData();

    public override IConquerorContextData UpstreamContextData { get; } = new DefaultConquerorContextData();

    public override IConquerorContextData ContextData { get; } = new DefaultConquerorContextData();

    public DefaultConquerorContext Clone(Action onClonedDispose)
    {
        var newContext = new DefaultConquerorContext(CopyUpstreamData);
        CopyDownstreamData();

        return newContext;

        void CopyDownstreamData()
        {
            // copy over all downstream data
            foreach (var (key, value, scope) in DownstreamContextData)
            {
                if (value is string s)
                {
                    newContext.DownstreamContextData.Set(key, s, scope);
                }
                else
                {
                    newContext.DownstreamContextData.Set(key, value);
                }
            }

            // copy over all bidirectional data
            foreach (var (key, value, scope) in ContextData)
            {
                if (value is string s)
                {
                    newContext.ContextData.Set(key, s, scope);
                }
                else
                {
                    newContext.ContextData.Set(key, value);
                }
            }
        }

        void CopyUpstreamData(ConquerorContext ctx)
        {
            foreach (var (key, value, scope) in ctx.UpstreamContextData)
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

            // clear parent context data before copying over data from child context, so that
            // removals are propagated from the child context
            ContextData.Clear();

            foreach (var (key, value, scope) in ctx.ContextData)
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

            onClonedDispose();
        }
    }

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            onDispose(this);
        }
    }
}
