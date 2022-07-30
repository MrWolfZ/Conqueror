using System.Collections.Generic;
using System.Threading;

namespace Conqueror.CQS.CommandHandling
{
    /// <inheritdoc />
    internal sealed class CommandClientContext : ICommandClientContext
    {
        private static readonly AsyncLocal<CommandClientContextHolder> ContextCurrent = new();

        /// <inheritdoc />
        public IDictionary<string, string> Items => ContextHolder.Items;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> ResponseItems => ContextHolder.ResponseItems;
        
        // null check to prevent unnecessary creation of async local
        public bool HasItems => ContextCurrent.Value != null && ContextHolder.Items.Count > 0;
        
        // null check to prevent unnecessary creation of async local
        public bool CaptureIsEnabled => ContextCurrent.Value != null && ContextHolder.CaptureIsEnabled;

        private CommandClientContextHolder ContextHolder => ContextCurrent.Value ?? (ContextCurrent.Value = new());

        /// <inheritdoc />
        public void CaptureResponseItems()
        {
            ContextHolder.CaptureIsEnabled = true;
        }

        public void AddResponseItems(IEnumerable<KeyValuePair<string, string>> source)
        {
            foreach (var p in source)
            {
                ContextHolder.ResponseItems[p.Key] = p.Value;
            }
        }

        private sealed class CommandClientContextHolder
        {
            public bool CaptureIsEnabled { get; set; }
            
            public Dictionary<string, string> Items { get; } = new();

            public Dictionary<string, string> ResponseItems { get; } = new();
        }
    }
}
