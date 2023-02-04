using System;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryTransportClientBuilder : IQueryTransportClientBuilder
    {
        public QueryTransportClientBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
    }
}
