using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.ServerSentEvents;
using Conqueror;
using Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using static Conqueror.ConquerorTransportHttpConstants;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpSseServerSignallingEndpointRouteBuilderExtensions
{
    [SuppressMessage("Minor Code Smell", "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
                     Justification = "not necessary for cancellation exceptions")]
    public static IEndpointConventionBuilder MapServerSentEventsSignalsEndpoint(this IEndpointRouteBuilder builder, string path)
    {
        return builder.MapGet(
            path,
            async context =>
            {
                var loggerFactory = builder.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Conqueror.HttpSseSignalEndpoint");

                var eventTypes = context.Request.Query.TryGetValue("signalTypes", out var st)
                    ? st.OfType<string>().Where(t => !string.IsNullOrWhiteSpace(t)).ToList()
                    : [];

                if (eventTypes.Count == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.Headers.Append(HeaderNames.ContentType, ContentTypes.TextPlain);
                    await context.Response.WriteAsync("must provide at least one signal type", context.RequestAborted).ConfigureAwait(false);

                    return;
                }

                try
                {
                    var singletons = context.RequestServices.GetRequiredService<ConquerorSingletons>();
                    var broker = singletons.GetOrAddSingleton(p => new HttpSseSignalBroker(p));

                    var items = broker.Subscribe(eventTypes, context.RequestAborted);

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.Headers.Append(HeaderNames.ContentType, ContentTypes.EventStream);

                    // flush the response stream to ensure that the client receives the headers
                    await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);

                    await SseFormatter.WriteAsync(
                                          items,
                                          context.Response.Body,
                                          context.RequestAborted)
                                      .ConfigureAwait(false);
                }
                catch (Exception) when (context.RequestAborted.IsCancellationRequested)
                {
                    // nothing to do, the client just disconnected, which may have thrown spurious exceptions
                    logger.LogInformation("client disconnected from signal stream");
                }
                catch (Exception e)
                {
                    // we don't handle the error here since terminating the connection should cause the client to reconnect;
                    // any missed signals due to this are by the nature of this transport which does not buffer anything and
                    // provides no guarantees about receiving all signals
                    logger.LogError(e, "an error occurred while formatting signal stream");
                }
            });
    }
}
