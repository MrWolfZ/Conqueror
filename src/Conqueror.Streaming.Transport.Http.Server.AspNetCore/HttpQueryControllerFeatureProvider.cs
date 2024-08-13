using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Conqueror.Streaming.Common;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal sealed class HttpQueryControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly DynamicStreamingControllerFactory controllerFactory;
    private readonly IReadOnlyCollection<StreamingHandlerMetadata> metadata;

    public HttpQueryControllerFeatureProvider(DynamicStreamingControllerFactory controllerFactory, IEnumerable<StreamingHandlerMetadata> metadata)
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
            var controllerType = controllerFactory.Create(stream, stream.RequestType.GetCustomAttribute<HttpStreamingRequestAttribute>()!).GetTypeInfo();

            if (!feature.Controllers.Contains(controllerType))
            {
                feature.Controllers.Add(controllerType);
            }
        }
    }

    private IEnumerable<StreamingHandlerMetadata> GetHttpStreams() =>
        metadata.Where(m => m.RequestType.GetCustomAttributes(typeof(HttpStreamingRequestAttribute), true).Any());
}
