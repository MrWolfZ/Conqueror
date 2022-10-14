using System;
using System.Threading;

namespace Conqueror
{
    /// <summary>
    ///     Provides an implementation of <see cref="IConquerorContextAccessor" /> based on the current execution context.
    /// </summary>
    internal sealed class ConquerorContextAccessor : IConquerorContextAccessor
    {
        private static readonly AsyncLocal<ConquerorContextHolder> ConquerorContextCurrent = new();

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IDisposableConquerorContext GetOrCreate()
        {
            if (ConquerorContext != null)
            {
                // if there already is a context, we just wrap it without any disposal action
                return new DisposableConquerorContext(ConquerorContext);
            }

            // if we create the context, we make sure that disposing it causes the context to be cleared
            var context = new DefaultConquerorContext();
            var disposableContext = new DisposableConquerorContext(context, ClearContext);
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
}
