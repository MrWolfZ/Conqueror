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
        return ConquerorContext is not null ? new NoOpDisposeConquerorContext(ConquerorContext) : CreateContext();
    }

    public ConquerorContext CloneOrCreate() =>
        ConquerorContextCurrent.Value?.Context is { } ctx ? CreateChildContext(ctx) : CreateContext();

    public void Set(DefaultConquerorContext conquerorContext) => SetContextInAsyncLocal(conquerorContext);

    private static DefaultConquerorContext CreateContext()
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? ActivityTraceId.CreateRandom().ToString();

        var context = DefaultConquerorContext.CreateRootContext(traceId, static _ => ClearContextFromAsyncLocal());
        SetContextInAsyncLocal(context);

        return context;
    }

    private static DefaultConquerorContext CreateChildContext(DefaultConquerorContext parentContext)
    {
        var childContext = parentContext.CreateChildContext(() => RestoreParentContextInAsyncLocal(parentContext));
        SetContextInAsyncLocal(childContext);

        return childContext;
    }

    private static void SetContextInAsyncLocal(DefaultConquerorContext context)
    {
        // Use an object indirection to hold the ConquerorContext in the AsyncLocal,
        // so it can be cleared in all ExecutionContexts when it's cleared.
        ConquerorContextCurrent.Value = new ConquerorContextHolder { Context = context };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearContextFromAsyncLocal()
    {
        var holder = ConquerorContextCurrent.Value;

        if (holder is not null)
        {
            // Clear current ConquerorContext trapped in the AsyncLocals, as it's done.
            holder.Context = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RestoreParentContextInAsyncLocal(DefaultConquerorContext parentContext)
    {
        var holder = ConquerorContextCurrent.Value;

        if (holder is not null)
        {
            // Restore parent ConquerorContext when child context is disposed
            holder.Context = parentContext;
        }
    }

    private sealed record ConquerorContextHolder
    {
        public DefaultConquerorContext? Context { get; set; }
    }
}
