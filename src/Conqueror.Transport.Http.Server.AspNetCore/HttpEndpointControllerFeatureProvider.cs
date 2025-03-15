using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.Transport.Http.Server.AspNetCore;

internal sealed class HttpEndpointControllerFeatureProvider(IEnumerable<HttpMessageControllerRegistration> registrations)
    : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var controllerType in registrations.Select(r => r.ControllerType))
        {
            if (!feature.Controllers.Contains(controllerType))
            {
                feature.Controllers.Add(controllerType);
            }
        }
    }
}

internal sealed record HttpMessageControllerRegistration(TypeInfo ControllerType);
