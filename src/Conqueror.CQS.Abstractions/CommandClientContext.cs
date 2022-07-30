using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror.CQS
{
    /// <inheritdoc />
    internal sealed class CommandClientContext : ICommandClientContext
    {
        private static readonly AsyncLocal<CommandClientContextHolder> ContextCurrent = new();

        public bool HasItems => IsActive && ContextCurrent.Value!.Items.Count > 0;

        public IDictionary<string, string> GetItems()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("client context is not active");
            }
            
            return ContextCurrent.Value!.Items;
        }

        /// <inheritdoc />
        public IDictionary<string, string> Activate()
        {
            if (IsActive)
            {
                throw new InvalidOperationException("client context is already active");
            }
            
            ContextCurrent.Value = new();
            return ContextCurrent.Value.Items;
        }

        /// <inheritdoc />
        public bool IsActive => ContextCurrent.Value != null;

        public void SetItems(IEnumerable<KeyValuePair<string, string>> source)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("client context is not active");
            }
            
            foreach (var p in source)
            {
                ContextCurrent.Value!.Items[p.Key] = p.Value;
            }
        }

        private sealed class CommandClientContextHolder
        {
            public Dictionary<string, string> Items { get; } = new();
        }
    }
}
