using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.CQS
{
    internal sealed class QueryHandlerMetadata
    {
        public QueryHandlerMetadata(Type queryType, Type responseType, Type handlerType)
        {
            QueryType = queryType;
            ResponseType = responseType;
            HandlerType = handlerType;
            MiddlewareConfigurationAttributes = GetConfigurationAttribute(handlerType).ToList();
        }

        public Type QueryType { get; }

        public Type ResponseType { get; }

        public Type HandlerType { get; }

        public IReadOnlyCollection<QueryMiddlewareConfigurationAttribute> MiddlewareConfigurationAttributes { get; }

        private IEnumerable<QueryMiddlewareConfigurationAttribute> GetConfigurationAttribute(Type handlerType)
        {
            var executeQueryMethodName = nameof(IQueryHandler<object, object>.ExecuteQuery);
            var executeMethod = handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                           .FirstOrDefault(m => m.Name == executeQueryMethodName && m.GetParameters().FirstOrDefault()?.ParameterType == QueryType);
            
            return executeMethod?.GetCustomAttributes().OfType<QueryMiddlewareConfigurationAttribute>() ?? Enumerable.Empty<QueryMiddlewareConfigurationAttribute>();
        }
    }
}
