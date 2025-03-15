using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.Transport.Http.Server.AspNetCore;

[RequiresDynamicCode("Needs to create generic types at runtime")]
internal sealed class HttpEndpointReflectionControllerFeatureProvider(IMessageTransportRegistry messageTransportRegistry)
    : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var (messageType, responseType) in messageTransportRegistry.GetMessageTypesForTransportInterface<IHttpMessage>())
        {
            var controllerType = typeof(MessageApiController<,>).MakeGenericType(messageType, responseType).GetTypeInfo();

            if (!feature.Controllers.Contains(controllerType))
            {
                feature.Controllers.Add(controllerType);
            }
        }
    }
}
