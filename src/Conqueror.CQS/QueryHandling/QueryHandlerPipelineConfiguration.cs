using System;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryHandlerPipelineConfiguration
    {
        public QueryHandlerPipelineConfiguration(Type handlerType, Action<IQueryPipelineBuilder> configure)
        {
            HandlerType = handlerType;
            Configure = configure;
        }

        public Type HandlerType { get; }

        public Action<IQueryPipelineBuilder> Configure { get; }
    }
}
