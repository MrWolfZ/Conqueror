using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class HttpQueryControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly DynamicQueryControllerFactory controllerFactory;
        private readonly IQueryHandlerRegistry queryHandlerRegistry;

        public HttpQueryControllerFeatureProvider(DynamicQueryControllerFactory controllerFactory, IQueryHandlerRegistry queryHandlerRegistry)
        {
            this.controllerFactory = controllerFactory;
            this.queryHandlerRegistry = queryHandlerRegistry;
        }

        public void PopulateFeature(
            IEnumerable<ApplicationPart> parts,
            ControllerFeature feature)
        {
            foreach (var query in GetHttpQueries())
            {
                var controllerType = controllerFactory.Create(query, query.QueryType.GetCustomAttribute<HttpQueryAttribute>()!).GetTypeInfo();

                if (!feature.Controllers.Contains(controllerType))
                {
                    feature.Controllers.Add(controllerType);
                }
            }
        }

        private IEnumerable<QueryHandlerRegistration> GetHttpQueries() => queryHandlerRegistry.GetQueryHandlerRegistrations()
                                                                                              .Where(m => m.QueryType.GetCustomAttributes(typeof(HttpQueryAttribute), true).Any());
    }
}
