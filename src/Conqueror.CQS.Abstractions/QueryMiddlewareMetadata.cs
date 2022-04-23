using System;

namespace Conqueror.CQS
{
    internal sealed class QueryMiddlewareMetadata
    {
        public QueryMiddlewareMetadata(Type middlewareType, Type? configurationType)
        {
            MiddlewareType = middlewareType;
            ConfigurationType = configurationType;
        }

        public Type MiddlewareType { get; }

        public Type? ConfigurationType { get; }
    }
}
