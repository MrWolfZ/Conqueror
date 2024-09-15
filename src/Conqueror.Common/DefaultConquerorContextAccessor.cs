using System;
using System.Threading;

namespace Conqueror.Common;

/// <summary>
///     Provides an implementation of <see cref="IConquerorContextAccessor" /> based on the current execution context.
/// </summary>
internal sealed class DefaultConquerorContextAccessor : IConquerorContextAccessor
{
    private static readonly AsyncLocal<ConquerorContextHolder> ConquerorContextCurrent = new();

    public IConquerorContext? ConquerorContext
    {
        get => ConquerorContextCurrent.Value?.Context;
        private set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "conqueror context must not be null");
            }

            // Use an object indirection to hold the ConquerorContext in the AsyncLocal,
            // so it can be cleared in all ExecutionContexts when it's cleared.
            ConquerorContextCurrent.Value = new() { Context = value };
        }
    }

    public IDisposableConquerorContext GetOrCreate()
    {
        // if there already is a context, we just wrap it without any disposal action
        return ConquerorContext != null ? new(ConquerorContext) : CreateContext();
    }

    public IDisposableConquerorContext CloneOrCreate()
    {
        return ConquerorContext != null ? CloneContext() : CreateContext();
    }

    private DisposableConquerorContext CreateContext()
    {
        var context = new DefaultConquerorContext();
        context.InitializeTraceId();
        var disposableContext = new DisposableConquerorContext(context, ClearContext);
        ConquerorContext = disposableContext;
        return disposableContext;
    }

    private DisposableConquerorContext CloneContext()
    {
        var parentContext = ConquerorContext!;

        var context = new DefaultConquerorContext();

        // copy over all downstream data
        foreach (var (key, value, scope) in parentContext.DownstreamContextData)
        {
            if (value is string s)
            {
                context.DownstreamContextData.Set(key, s, scope);
            }
            else
            {
                context.DownstreamContextData.Set(key, value);
            }
        }

        // copy over all bidirectional data
        foreach (var (key, value, scope) in parentContext.ContextData)
        {
            if (value is string s)
            {
                context.ContextData.Set(key, s, scope);
            }
            else
            {
                context.ContextData.Set(key, value);
            }
        }

        // copy all upstream and bidirectional data to the parent context when this context is disposed
        var disposableContext = new DisposableConquerorContext(context, () =>
        {
            foreach (var (key, value, scope) in context.UpstreamContextData)
            {
                if (value is string s)
                {
                    parentContext.UpstreamContextData.Set(key, s, scope);
                }
                else
                {
                    parentContext.UpstreamContextData.Set(key, value);
                }
            }

            // clear parent context data before copying over data from child context, so that
            // removals are propagated from the child context
            parentContext.ContextData.Clear();

            foreach (var (key, value, scope) in context.ContextData)
            {
                if (value is string s)
                {
                    parentContext.ContextData.Set(key, s, scope);
                }
                else
                {
                    parentContext.ContextData.Set(key, value);
                }
            }

            ClearContext();
        });

        ConquerorContext = disposableContext;
        return disposableContext;
    }

    private static void ClearContext()
    {
        var holder = ConquerorContextCurrent.Value;

        if (holder != null)
        {
            // Clear current ConquerorContext trapped in the AsyncLocals, as it's done.
            holder.Context = null;
        }
    }

    private sealed class ConquerorContextHolder
    {
        public IConquerorContext? Context { get; set; }
    }
}
