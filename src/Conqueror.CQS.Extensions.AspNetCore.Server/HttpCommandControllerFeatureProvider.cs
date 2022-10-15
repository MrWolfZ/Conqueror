using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Conqueror.CQS.Common;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    internal sealed class HttpCommandControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly DynamicCommandControllerFactory controllerFactory;
        private readonly IReadOnlyCollection<CommandHandlerMetadata> metadata;

        public HttpCommandControllerFeatureProvider(DynamicCommandControllerFactory controllerFactory, IEnumerable<CommandHandlerMetadata> metadata)
        {
            this.controllerFactory = controllerFactory;
            this.metadata = metadata.ToList();
        }

        public void PopulateFeature(
            IEnumerable<ApplicationPart> parts,
            ControllerFeature feature)
        {
            foreach (var command in GetHttpCommands())
            {
                var controllerType = controllerFactory.Create(command, command.CommandType.GetCustomAttribute<HttpCommandAttribute>()!).GetTypeInfo();

                if (!feature.Controllers.Contains(controllerType))
                {
                    feature.Controllers.Add(controllerType);
                }
            }
        }

        private IEnumerable<CommandHandlerMetadata> GetHttpCommands() => metadata.Where(m => m.CommandType.GetCustomAttributes(typeof(HttpCommandAttribute), true).Any());
    }
}
