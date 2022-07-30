using System;
using System.Threading;

namespace Conqueror.CQS.CommandHandling
{
    /// <summary>
    ///     Provides an implementation of <see cref="ICommandContextAccessor" /> based on the current execution context.
    /// </summary>
    internal sealed class CommandContextAccessor : ICommandContextAccessor
    {
        private static readonly AsyncLocal<CommandContextHolder> CommandContextCurrent = new();

        /// <inheritdoc />
        public ICommandContext? CommandContext
        {
            get => CommandContextCurrent.Value?.Context;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "command context must not be null");
                }

                // Use an object indirection to hold the CommandContext in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                CommandContextCurrent.Value = new() { Context = value };
            }
        }

        public void ClearContext()
        {
            var holder = CommandContextCurrent.Value;

            if (holder != null)
            {
                // Clear current CommandContext trapped in the AsyncLocals, as it's done.
                holder.Context = null;
            }
        }

        private sealed class CommandContextHolder
        {
            public ICommandContext? Context { get; set; }
        }
    }
}
