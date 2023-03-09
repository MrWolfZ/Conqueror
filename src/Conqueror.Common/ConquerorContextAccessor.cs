using System;
using System.Diagnostics;
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
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "conqueror context must not be null");
            }

            // Use an object indirection to hold the ConquerorContext in the AsyncLocal,
            // so it can be cleared in all ExecutionContexts when its cleared.
            ConquerorContextCurrent.Value = new() { Context = value };
        }
    }

    public IDisposableConquerorContext GetOrCreate()
    {
        // if there already is a context, we just wrap it without any disposal action
        return ConquerorContext != null ? new DisposableConquerorContext(ConquerorContext) : CreateContext();
    }

    public IDisposableConquerorContext CloneOrCreate()
    {
        return ConquerorContext != null ? CloneContext() : CreateContext();
    }

    private IDisposableConquerorContext CreateContext()
    {
        // if we create the context, we make sure that disposing it causes the context to be cleared
        var context = new DefaultConquerorContext(Activity.Current?.TraceId.ToString() ?? ActivityTraceId.CreateRandom().ToString());
        var disposableContext = new DisposableConquerorContext(context, ClearContext);
        ConquerorContext = context;
        return disposableContext;
    }

    private IDisposableConquerorContext CloneContext()
    {
        var parentContext = ConquerorContext!;

        var context = new DefaultConquerorContext(parentContext.TraceId);

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

        // copy all upstream data to the parent context when this context is disposed
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

            ClearContext();
        });

        ConquerorContext = context;
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
