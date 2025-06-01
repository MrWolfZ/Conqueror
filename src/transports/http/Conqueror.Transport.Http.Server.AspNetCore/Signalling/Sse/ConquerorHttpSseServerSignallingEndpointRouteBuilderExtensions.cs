using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Conqueror.Transport.Http.Server.AspNetCore;
using Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpSseServerSignallingEndpointRouteBuilderExtensions
{
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "not necessary for cancellation exceptions")]
    public static IEndpointConventionBuilder MapServerSentEventsSignalsEndpoint(this IEndpointRouteBuilder builder, string path)
    {
        return builder.MapGet(
            path,
            async context =>
            {
                var loggerFactory = builder.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Conqueror.HttpSseSignalEndpoint");

                var eventTypes = context.Request.Query.TryGetValue(QueryParameterNames.SignalSseEventType, out var st)
                    ? st.OfType<string>().Where(t => !string.IsNullOrWhiteSpace(t)).ToList()
                    : [];

                if (eventTypes.Count == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.Headers.Append(HeaderNames.ContentType, ContentTypes.TextPlain);
                    await context.Response.WriteAsync("must provide at least one signal type", context.RequestAborted).ConfigureAwait(false);

                    return;
                }

                await SseEndpoint.Run(
                                     context,
                                     logger,
                                     () => context.RequestServices
                                                  .GetRequiredService<HttpSseSignalBroker>()
                                                  .Subscribe(eventTypes))
                                 .ConfigureAwait(false);
            });
    }
}
