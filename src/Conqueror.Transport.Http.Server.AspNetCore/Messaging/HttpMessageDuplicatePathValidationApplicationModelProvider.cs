using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

internal sealed class HttpMessageDuplicatePathValidationApplicationModelProvider : IApplicationModelProvider
{
    public int Order => 0;

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        // nothing to do
    }

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        var sb = new StringBuilder();

        // this is a sanity check that should never fail under normal circumstances
        var messageTypesWithoutTemplate = (from controller in context.Result.Controllers
                                           where (controller.ControllerType.BaseType?.IsGenericType ?? false)
                                                 && controller.ControllerType.BaseType.GetGenericTypeDefinition() == typeof(MessageApiControllerBase<,>)
                                           let messageType = controller.ControllerType.GetGenericArguments()[0]
                                           from action in controller.Actions
                                           from selector in action.Selectors
                                           let path = selector.AttributeRouteModel?.Template
                                           where path is null
                                           select messageType).ToList();

        if (messageTypesWithoutTemplate.Count > 0)
        {
            throw new InvalidOperationException($"found Conqueror message types without template: {string.Join(", ", messageTypesWithoutTemplate)}");
        }

        foreach (var (path, messageTypes) in
                 from controller in context.Result.Controllers
                 where (controller.ControllerType.BaseType?.IsGenericType ?? false)
                       && controller.ControllerType.BaseType.GetGenericTypeDefinition() == typeof(MessageApiControllerBase<,>)
                 let messageType = controller.ControllerType.GetGenericArguments()[0]
                 from action in controller.Actions
                 from selector in action.Selectors
                 let path = selector.AttributeRouteModel?.Template
                 group messageType by path
                 into pathGroup
                 let path = pathGroup.Key
                 let messageTypes = pathGroup.ToList()
                 where messageTypes.Count > 1
                 select (path, messageTypes))
        {
            if (sb.Length > 0)
            {
                _ = sb.Append('\n');
            }

            _ = sb.Append($"path: {path}, messageTypes: {string.Join(", ", messageTypes)}");
        }

        if (sb.Length > 0)
        {
            throw new InvalidOperationException($"found multiple Conqueror message types with identical path!\n{sb}");
        }
    }
}
