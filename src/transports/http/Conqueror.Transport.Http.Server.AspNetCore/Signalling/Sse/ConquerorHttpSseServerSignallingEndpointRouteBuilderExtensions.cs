using System;
using System.Linq;
using System.Net.ServerSentEvents;
using Conqueror;
using Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpSseServerSignallingEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapServerSentEventsSignalsEndpoint(this IEndpointRouteBuilder builder, string path)
    {
        return builder.MapGet(
            path,
            async context =>
            {
                var singletons = context.RequestServices.GetRequiredService<ConquerorSingletons>();
                var broker = singletons.GetOrAddSingleton(p => new HttpSseSignalBroker(p));

                var eventTypes = context.Request.Query.TryGetValue("signalTypes", out var st)
                    ? st.OfType<string>().Where(t => !string.IsNullOrWhiteSpace(t)).ToList()
                    : [];

                if (eventTypes.Count == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.Headers.Append("Content-Type", "text/plain");
                    await context.Response.WriteAsync("must provide at least one signal type", context.RequestAborted).ConfigureAwait(false);

                    return;
                }

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.Headers.Append("Content-Type", "text/event-stream");

                var (items, formatter) = broker.Subscribe(eventTypes, context.RequestAborted);

                // flush the response stream to ensure that the client receives the headers
                await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);

                try
                {
                    await SseFormatter.WriteAsync(
                                          items,
                                          context.Response.Body,
                                          formatter,
                                          context.RequestAborted)
                                      .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // nothing to do, we just completed the request
                }
                catch (Exception e)
                {
                    // TODO: write error to response stream and properly log it using ILogger
                    Console.WriteLine(e);
                }
            });
    }
}
