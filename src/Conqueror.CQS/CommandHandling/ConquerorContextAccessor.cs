using System;
using System.Threading;

namespace Conqueror.CQS.CommandHandling
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

        public void ClearContext()
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
