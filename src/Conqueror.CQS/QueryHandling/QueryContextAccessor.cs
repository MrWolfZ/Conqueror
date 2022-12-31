using System;
using System.Threading;

namespace Conqueror.CQS.QueryHandling
{
    /// <summary>
    ///     Provides an implementation of <see cref="IQueryContextAccessor" /> based on the current execution context.
    /// </summary>
    internal sealed class QueryContextAccessor : IQueryContextAccessor
    {
        private static readonly AsyncLocal<QueryContextHolder> QueryContextCurrent = new();

        private string? externalQueryId;

        /// <inheritdoc />
        public IQueryContext? QueryContext
        {
            get => QueryContextCurrent.Value?.Context;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "query context must not be null");
                }

                // Use an object indirection to hold the QueryContext in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                QueryContextCurrent.Value = new() { Context = value };
            }
        }

        /// <inheritdoc />
        public void SetExternalQueryId(string queryId)
        {
            externalQueryId = queryId;
        }

        public string? DrainExternalQueryId()
        {
            var queryId = externalQueryId;
            externalQueryId = null;
            return queryId;
        }

        public void ClearContext()
        {
            var holder = QueryContextCurrent.Value;

            if (holder != null)
            {
                // Clear current QueryContext trapped in the AsyncLocals, as it's done.
                holder.Context = null;
            }
        }

        private sealed class QueryContextHolder
        {
            public IQueryContext? Context { get; set; }
        }
    }
}
