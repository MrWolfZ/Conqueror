#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpWebSocketsServerSignallingEndpointRouteBuilderExtensions
{
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "not necessary for cancellation exceptions"
    )]
    public static IEndpointConventionBuilder MapWebSocketsSignalsEndpoint(
        this IEndpointRouteBuilder builder,
        string path
    )
    {
        return builder.MapGet(
            path,
            async context =>
            {
                var loggerFactory = builder.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Conqueror.HttpWebSocketsSignalEndpoint");

                var tags = context.Request.Query.TryGetValue(QueryParameterNames.SignalWebSocketsTag, out var st)
                    ? st.OfType<string>().Where(t => !string.IsNullOrWhiteSpace(t)).ToList()
                    : [];

                if (tags.Count is 0)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.Headers.Append(HeaderNames.ContentType, ContentTypes.TextPlain);
                    await context
                        .Response.WriteAsync("must provide at least one signal tag", context.RequestAborted)
                        .ConfigureAwait(false);

                    return;
                }

                if (
                    !WebSocketEndpoint.TryGetHeartbeatParameters(
                        context,
                        out var heartbeatInterval,
                        out var heartbeatTimeout,
                        out var message
                    )
                )
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.Headers.Append(HeaderNames.ContentType, ContentTypes.TextPlain);
                    await context.Response.WriteAsync(message, context.RequestAborted).ConfigureAwait(false);
                }

                var tcs = new TaskCompletionSource<WebSocketWriteStream>();

                try
                {
                    var stream = new HttpWebSocketsSignalBrokerStream(tcs.Task);

                    using var sub = context
                        .RequestServices.GetRequiredService<HttpWebSocketsSignalBroker>()
                        .Subscribe(stream, tags, context.RequestAborted);

                    await WebSocketEndpoint
                        .Run(context, logger, heartbeatInterval, heartbeatTimeout, s => tcs.TrySetResult(s))
                        .ConfigureAwait(false);
                }
                finally
                {
                    _ = tcs.TrySetCanceled(CancellationToken.None);
                }
            }
        );
    }
}
