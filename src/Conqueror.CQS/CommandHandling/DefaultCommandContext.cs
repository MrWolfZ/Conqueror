using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror.CQS.CommandHandling
{
    /// <inheritdoc />
    internal sealed class DefaultCommandContext : ICommandContext
    {
        private readonly Lazy<IDictionary<object, object?>> itemsLazy = new(() => new ConcurrentDictionary<object, object?>());

        private object command;
        private object? response;

        public DefaultCommandContext(object command)
        {
            this.command = command;
        }

        /// <inheritdoc />
        public object Command => command;

        /// <inheritdoc />
        public object? Response => response;

        /// <inheritdoc />
        public IDictionary<object, object?> Items => itemsLazy.Value;

        public void SetCommand(object cmd)
        {
            command = cmd;
        }

        public void SetResponse(object? res)
        {
            response = res;
        }
    }
}
