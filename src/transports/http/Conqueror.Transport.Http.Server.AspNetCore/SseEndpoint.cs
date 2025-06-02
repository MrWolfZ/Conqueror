using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Conqueror.Transport.Http.Server.AspNetCore;

internal static partial class SseEndpoint
{
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "not necessary for cancellation exceptions")]
    public static async Task Run(
        HttpContext context,
        ILogger logger,
        Func<IAsyncEnumerable<SseItem<string>>> getItems)
    {
        try
        {
            var items = getItems();

            if (context.Features.Get<IHttpResponseBodyFeature>() is { } f)
            {
                f.DisableBuffering();
            }

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = ContentTypes.EventStream;

            // flush the response stream to ensure that the client receives the headers
            await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);

            await SseFormatter.WriteAsync(
                                  RunWithFlushing(items, context.Response.Body, logger),
                                  context.Response.Body,
                                  context.RequestAborted)
                              .ConfigureAwait(false);

            static async IAsyncEnumerable<SseItem<string>> RunWithFlushing(
                IAsyncEnumerable<SseItem<string>> items,
                Stream responseBody,
                ILogger logger,
                [EnumeratorCancellation] CancellationToken ct = default)
            {
                await foreach (var item in items.ConfigureAwait(false).WithCancellation(ct))
                {
                    yield return item;

                    LogFlush(logger, item.EventType);

                    // flush the response body after each item (which unfortunately isn't done automatically
                    // in the SseFormatter
                    await responseBody.FlushAsync(ct).ConfigureAwait(false);
                }
            }
        }
        catch (Exception) when (context.RequestAborted.IsCancellationRequested)
        {
            // nothing to do, the client just disconnected, which may have thrown spurious exceptions
            logger.LogDebug("client disconnected from event stream");
        }
        catch (Exception e)
        {
            // we don't handle the error here since terminating the connection should cause the client to reconnect;
            // any missed items due to this are by the nature of this transport which does not buffer anything and
            // provides no guarantees about receiving all items
            logger.LogError(e, "an error occurred while formatting event stream");
        }
    }

    [LoggerMessage(LogLevel.Trace, "flushing item of event type '{EventType}'")]
    private static partial void LogFlush(ILogger logger, string eventType);
}
