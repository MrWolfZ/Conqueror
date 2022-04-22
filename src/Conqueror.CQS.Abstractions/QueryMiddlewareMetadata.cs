using System;

namespace Conqueror.CQS
{
    internal sealed class QueryMiddlewareMetadata
    {
        public QueryMiddlewareMetadata(Type middlewareType, Type attributeType)
        {
            MiddlewareType = middlewareType;
            AttributeType = attributeType;
        }

        public Type MiddlewareType { get; }

        public Type AttributeType { get; }
    }
}
