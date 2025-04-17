using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

internal sealed class HttpMessageEndpointApplicationModelProvider<TMessage, TResponse>(ILoggerFactory loggerFactory) : IApplicationModelProvider
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    /// <summary>
    ///     The <see cref="Microsoft.AspNetCore.Mvc.ApiControllerAttribute" /> attribute
    ///     model provider uses <c>-1000 + 100</c> - this needs to be applied first so
    ///     that we get attribute routing.
    /// </summary>
    public int Order => -1000 + 51;

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        var logger = loggerFactory.CreateLogger<HttpMessageEndpointApplicationModelProvider<TMessage, TResponse>>();

        foreach (var (controller, action, selectorModel) in
                 from controller in context.Result.Controllers
                 where controller.ControllerType.IsAssignableTo(typeof(MessageApiControllerBase<TMessage, TResponse>))
                 from action in controller.Actions
                 from selector in action.Selectors
                 select (controller, action, selector))
        {
            logger.LogDebug("configuring message controller for message type '{MessageType}'", typeof(TMessage));

            controller.ControllerName = "ConquerorMessageController";
            action.ActionName = TMessage.Name;

            selectorModel.AttributeRouteModel ??= new();

            ConfigureName();
            ConfigurePath();
            ConfigureMethod();
            ConfigureApiGroupName();
            ConfigureSuccessStatusCode();
            ConfigureAcceptedContentTypes();

            void ConfigureName()
            {
                if (selectorModel.AttributeRouteModel.Name is { } name)
                {
                    logger.LogTrace("message controller for message type '{MessageType}' already has name configured: {Name}",
                                    typeof(TMessage),
                                    name);
                }
                else
                {
                    logger.LogTrace("Setting name for message controller for message type '{MessageType}': {Name}",
                                    typeof(TMessage),
                                    TMessage.Name);

                    selectorModel.AttributeRouteModel.Name = TMessage.Name;
                }
            }

            void ConfigurePath()
            {
                if (selectorModel.AttributeRouteModel.Template is { } template)
                {
                    logger.LogTrace("message controller for message type '{MessageType}' already has path configured: {FullPath}",
                                    typeof(TMessage),
                                    template);
                }
                else
                {
                    logger.LogTrace("Setting path for message controller for message type '{MessageType}': {FullPath}",
                                    typeof(TMessage),
                                    TMessage.FullPath);

                    selectorModel.AttributeRouteModel.Template = TMessage.FullPath;
                }
            }

            void ConfigureMethod()
            {
                var httpMethodConstraint = selectorModel.ActionConstraints.OfType<HttpMethodActionConstraint?>().FirstOrDefault();

                if (httpMethodConstraint is not null)
                {
                    logger.LogTrace("message controller for message type '{MessageType}' already has methods configured: {Methods}",
                                    typeof(TMessage),
                                    string.Join(",", httpMethodConstraint.HttpMethods));
                }
                else
                {
                    logger.LogTrace("Setting method for message controller for message type '{MessageType}': {Method}",
                                    typeof(TMessage),
                                    TMessage.HttpMethod);

                    selectorModel.ActionConstraints.Add(
                        new HttpMethodActionConstraint([TMessage.HttpMethod]));
                }
            }

            void ConfigureApiGroupName()
            {
                if (action.ApiExplorer.GroupName is not null)
                {
                    logger.LogTrace("message controller for message type '{MessageType}' already has success API group name configured: {GroupName}",
                                    typeof(TMessage),
                                    action.ApiExplorer.GroupName);
                }
                else
                {
                    logger.LogTrace("Setting API group name for message controller for message type '{MessageType}': {GroupName}",
                                    typeof(TMessage),
                                    TMessage.ApiGroupName);

                    action.ApiExplorer.GroupName = TMessage.ApiGroupName;

                    // Swashbuckle uses this by default to extract tags, which then result in grouping in the UI
                    action.RouteValues["controller"] = TMessage.ApiGroupName ?? TMessage.Name;
                }
            }

            void ConfigureSuccessStatusCode()
            {
                var statusCode = action.Filters.OfType<IApiResponseMetadataProvider>().Select(p => p.StatusCode).FirstOrDefault();

                if (statusCode > 0)
                {
                    logger.LogTrace("message controller for message type '{MessageType}' already has success status code configured: {StatusCode}",
                                    typeof(TMessage),
                                    statusCode);
                }
                else
                {
                    logger.LogTrace("Setting success status code for message controller for message type '{MessageType}': {StatusCode}",
                                    typeof(TMessage),
                                    TMessage.SuccessStatusCode);

                    if (typeof(TResponse) == typeof(UnitMessageResponse))
                    {
                        action.Filters.Add(new ProducesResponseTypeAttribute(TMessage.SuccessStatusCode));
                    }
                    else
                    {
                        action.Filters.Add(new ProducesResponseTypeAttribute(typeof(TResponse), TMessage.SuccessStatusCode, MediaTypeNames.Application.Json));
                    }
                }
            }

            void ConfigureAcceptedContentTypes()
            {
                if (action.Filters.OfType<ConsumesAttribute>().Any())
                {
                    logger.LogTrace("message controller for message type '{MessageType}' already has consume attribute configured",
                                    typeof(TMessage));
                }
                else
                {
                    logger.LogTrace("Setting consumed content type for message controller for message type '{MessageType}'",
                                    typeof(TMessage));

                    if (action.Parameters.Any(p => p.BindingInfo?.BindingSource == BindingSource.Body))
                    {
                        action.Filters.Add(new ConsumesAttribute(MediaTypeNames.Application.Json));
                    }
                }
            }
        }
    }

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        // nothing to do
    }
}
