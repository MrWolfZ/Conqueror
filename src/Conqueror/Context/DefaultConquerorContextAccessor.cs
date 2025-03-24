using System.Threading;

namespace Conqueror.Context;

/// <summary>
///     Provides an implementation of <see cref="IConquerorContextAccessor" /> based on the current execution context.
/// </summary>
internal sealed class DefaultConquerorContextAccessor : IConquerorContextAccessor
{
    private static readonly AsyncLocal<ConquerorContextHolder> ConquerorContextCurrent = new();

    public ConquerorContext? ConquerorContext => DefaultConquerorContext;

    private static DefaultConquerorContext? DefaultConquerorContext => ConquerorContextCurrent.Value?.Context;

    public ConquerorContext GetOrCreate()
    {
        // if there already is a context, we just wrap it without any disposal action
        return ConquerorContext != null ? new NoOpDisposeConquerorContext(ConquerorContext) : CreateContext();
    }

    public ConquerorContext CloneOrCreate()
    {
        return DefaultConquerorContext != null ? CloneContext(DefaultConquerorContext) : CreateContext();
    }

    private static DefaultConquerorContext CreateContext()
    {
        var context = new DefaultConquerorContext(_ => ClearContextFromAsyncLocal());
        context.InitializeTraceId();
        SetContextInAsyncLocal(context);
        return context;
    }

    private static DefaultConquerorContext CloneContext(DefaultConquerorContext parentContext)
    {
        var clonedContext = parentContext.Clone(ClearContextFromAsyncLocal);
        SetContextInAsyncLocal(clonedContext);
        return clonedContext;
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
