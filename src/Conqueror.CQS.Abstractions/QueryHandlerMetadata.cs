using System;

namespace Conqueror
{
    internal sealed class QueryHandlerMetadata
    {
        public QueryHandlerMetadata(Type queryType, Type responseType, Type handlerType)
        {
            QueryType = queryType;
            ResponseType = responseType;
            HandlerType = handlerType;
        }

        public Type QueryType { get; }

        public Type ResponseType { get; }

        public Type HandlerType { get; }
    }
}
