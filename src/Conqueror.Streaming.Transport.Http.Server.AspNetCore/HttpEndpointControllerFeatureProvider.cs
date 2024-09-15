using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal sealed class HttpEndpointControllerFeatureProvider(
    IReadOnlyCollection<HttpEndpoint> endpoints)
    : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts,
                                ControllerFeature feature)
    {
        foreach (var endpoint in endpoints)
        {
            var controllerType = DynamicStreamingEndpointControllerFactory.Create(endpoint).GetTypeInfo();

            if (!feature.Controllers.Contains(controllerType))
            {
                feature.Controllers.Add(controllerType);
            }
        }
    }
}
