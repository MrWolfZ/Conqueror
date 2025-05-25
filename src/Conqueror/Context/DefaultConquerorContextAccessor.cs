using System.Threading;

namespace Conqueror.Context;

/// <summary>
///     Provides an implementation of <see cref="IConquerorContextAccessor" /> based on the current execution context.
/// </summary>
internal sealed class DefaultConquerorContextAccessor : IConquerorContextAccessor
{
    private static readonly AsyncLocal<ConquerorContextHolder> ConquerorContextCurrent = new();

    public ConquerorContext? ConquerorContext => ConquerorContextCurrent.Value?.Context;

    public ConquerorContext GetOrCreate()
    {
        // if there already is a context, we just wrap it without any disposal action
        return ConquerorContext != null ? new NoOpDisposeConquerorContext(ConquerorContext) : CreateContext();
    }

    public ConquerorContext CloneOrCreate()
    {
        return ConquerorContextCurrent.Value?.Context is { } ctx ? CreateChildContext(ctx) : CreateContext();
    }

    private static DefaultConquerorContext CreateContext()
    {
        var context = DefaultConquerorContext.CreateRootContext(_ => ClearContextFromAsyncLocal());
        context.InitializeTraceId();
        SetContextInAsyncLocal(context);
        return context;
    }

    private static DefaultConquerorContext CreateChildContext(DefaultConquerorContext parentContext)
    {
        var childContext = parentContext.CreateChildContext(ClearContextFromAsyncLocal);
        SetContextInAsyncLocal(childContext);
        return childContext;
    }

    private static void SetContextInAsyncLocal(DefaultConquerorContext context)
    {
        // Use an object indirection to hold the ConquerorContext in the AsyncLocal,
        // so it can be cleared in all ExecutionContexts when it's cleared.
        ConquerorContextCurrent.Value = new() { Context = context };
    }

    private static void ClearContextFromAsyncLocal()
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
        public DefaultConquerorContext? Context { get; set; }
    }
}
