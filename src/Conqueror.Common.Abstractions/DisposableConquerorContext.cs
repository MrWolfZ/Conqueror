using System;
using System.Collections.Generic;

namespace Conqueror
{
    internal sealed class DisposableConquerorContext : IDisposableConquerorContext
    {
        private readonly Action? onDispose;
        private readonly IConquerorContext wrappedContext;

        public DisposableConquerorContext(IConquerorContext wrappedContext, Action? onDispose = null)
        {
            this.onDispose = onDispose;
            this.wrappedContext = wrappedContext;
        }

        /// <inheritdoc />
        public IDictionary<string, string> Items => wrappedContext.Items;

        public void Dispose() => onDispose?.Invoke();
    }
}
