using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class HttpEndpointControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly HttpEndpointRegistry endpointRegistry;

        public HttpEndpointControllerFeatureProvider(HttpEndpointRegistry endpointRegistry)
        {
            this.endpointRegistry = endpointRegistry;
        }

        public void PopulateFeature(
            IEnumerable<ApplicationPart> parts,
            ControllerFeature feature)
        {
            foreach (var endpoint in endpointRegistry.GetEndpoints())
            {
                var controllerType = DynamicCqsEndpointControllerFactory.Create(endpoint).GetTypeInfo();

                if (!feature.Controllers.Contains(controllerType))
                {
                    feature.Controllers.Add(controllerType);
                }
            }
        }
    }
}
