using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal sealed class HttpQueryControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly DynamicStreamingControllerFactory controllerFactory;
    private readonly IReadOnlyCollection<StreamingRequestHandlerRegistration> metadata;

    public HttpQueryControllerFeatureProvider(DynamicStreamingControllerFactory controllerFactory, IEnumerable<StreamingRequestHandlerRegistration> metadata)
    {
        this.controllerFactory = controllerFactory;
        this.metadata = metadata.ToList();
    }

    public void PopulateFeature(
        IEnumerable<ApplicationPart> parts,
        ControllerFeature feature)
    {
        foreach (var stream in GetHttpStreams())
        {
            var controllerType = controllerFactory.Create(stream, stream.StreamingRequestType.GetCustomAttribute<HttpStreamingRequestAttribute>()!).GetTypeInfo();

            if (!feature.Controllers.Contains(controllerType))
            {
                feature.Controllers.Add(controllerType);
            }
        }
    }

    private IEnumerable<StreamingRequestHandlerRegistration> GetHttpStreams() =>
        metadata.Where(m => m.StreamingRequestType.GetCustomAttributes(typeof(HttpStreamingRequestAttribute), true).Any());
}
