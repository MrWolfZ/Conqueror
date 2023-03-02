using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal sealed class HttpEndpointControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly IReadOnlyCollection<HttpEndpoint> endpoints;

    public HttpEndpointControllerFeatureProvider(IReadOnlyCollection<HttpEndpoint> endpoints)
    {
        this.endpoints = endpoints;
    }

    public void PopulateFeature(
        IEnumerable<ApplicationPart> parts,
        ControllerFeature feature)
    {
        foreach (var endpoint in endpoints)
        {
            var controllerType = DynamicCqsEndpointControllerFactory.Create(endpoint).GetTypeInfo();

            if (!feature.Controllers.Contains(controllerType))
            {
                feature.Controllers.Add(controllerType);
            }
        }
    }
}
