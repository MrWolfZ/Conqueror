using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class HttpCommandControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly DynamicCommandControllerFactory controllerFactory;
        private readonly ICommandHandlerRegistry commandHandlerRegistry;

        public HttpCommandControllerFeatureProvider(DynamicCommandControllerFactory controllerFactory, ICommandHandlerRegistry commandHandlerRegistry)
        {
            this.controllerFactory = controllerFactory;
            this.commandHandlerRegistry = commandHandlerRegistry;
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

        private IEnumerable<CommandHandlerRegistration> GetHttpCommands() => commandHandlerRegistry.GetCommandHandlerRegistrations()
                                                                                                   .Where(m => m.CommandType.GetCustomAttributes(typeof(HttpCommandAttribute), true).Any());
    }
}
