using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Conqueror.Streaming.Interactive.Common;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Server
{
    internal sealed class HttpQueryControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly DynamicInteractiveStreamingControllerFactory controllerFactory;
        private readonly IReadOnlyCollection<InteractiveStreamingHandlerMetadata> metadata;

        public HttpQueryControllerFeatureProvider(DynamicInteractiveStreamingControllerFactory controllerFactory, IEnumerable<InteractiveStreamingHandlerMetadata> metadata)
        {
            this.controllerFactory = controllerFactory;
            this.metadata = metadata.ToList();
        }

        public void PopulateFeature(
            IEnumerable<ApplicationPart> parts,
            ControllerFeature feature)
        {
            foreach (var interactiveStream in GetHttpInteractiveStreams())
            {
                var controllerType = controllerFactory.Create(interactiveStream, interactiveStream.RequestType.GetCustomAttribute<HttpInteractiveStreamingRequestAttribute>()!).GetTypeInfo();

                if (!feature.Controllers.Contains(controllerType))
                {
                    feature.Controllers.Add(controllerType);
                }
            }
        }

        private IEnumerable<InteractiveStreamingHandlerMetadata> GetHttpInteractiveStreams() =>
            metadata.Where(m => m.RequestType.GetCustomAttributes(typeof(HttpInteractiveStreamingRequestAttribute), true).Any());
    }
}
