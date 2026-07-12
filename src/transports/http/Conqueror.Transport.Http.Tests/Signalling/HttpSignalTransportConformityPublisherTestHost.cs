namespace Conqueror.Transport.Http.Tests.Signalling;

public sealed class HttpSignalTransportConformityPublisherTestHost : ISignalTransportConformityPublisherTestHost
{
    private readonly CancellationTokenSource serverCts = new();

    private int serverCallCount;
    private CancellationToken? serverCancellationToken;
    private int serverResponseHasBegunCount;
    private int serverResponseHasFinishedCount;

    private HttpTransportTestHost HttpTransportTestHost { get; set; } = null!;

    public int ServerCallCount => serverCallCount;

    [SuppressMessage(
        "Roslynator",
        "RCS1085:Use auto-implemented property",
        Justification = "false positive, we need the field as a `ref` parameter"
    )]
    public int ServerResponseHasBegunCount
    {
        get => serverResponseHasBegunCount;
        set => serverResponseHasBegunCount = value;
    }

    public int ServerResponseHasFinishedCount => serverResponseHasFinishedCount;

    public IHeaderDictionary? ReceivedHeadersOnServer { get; private set; }

    public ConcurrentQueue<(int StatusCode, string ContentType, bool KeepAlive)?> ServerConnectionResponses { get; } =
        [];

    public HttpClient HttpClient => HttpTransportTestHost.HttpClient;

    public ISignalPublishers SignalPublishers => HttpTransportTestHost.Resolve<ISignalPublishers>();

    public IConquerorContextAccessor ConquerorContextAccessor =>
        HttpTransportTestHost.Resolve<IConquerorContextAccessor>();

    public async ValueTask DisposeAsync()
    {
        serverCts.Dispose();

        await HttpTransportTestHost.DisposeAsync();
    }

    public static async Task<HttpSignalTransportConformityPublisherTestHost> CreatePublisherHost(
        HttpSignalConformityTestCase testCase,
        Func<object, ConquerorContext, CancellationToken, Task>? publishCallback
    )
    {
        var host = new HttpSignalTransportConformityPublisherTestHost();

        host.HttpTransportTestHost = await HttpTransportTestHost.Create(
            services =>
            {
                testCase.RegisterOnServer?.Invoke(services);

                _ = services
                    .AddConquerorHttpServerAspNetCore()
                    .AddRouting()
                    .AddSingleton(ILogger (p) => p.GetRequiredService<ILogger<HttpSignalTransportConformityTestHost>>()
                    );

                if (publishCallback is not null)
                {
                    _ = services.AddSingleton(publishCallback);
                }

                testCase.RegisterServerServices(services);
            },
            app =>
            {
                _ = app.Use(async (ctx, next) =>
                        {
                            _ = Interlocked.Increment(ref host.serverCallCount);
                            host.ReceivedHeadersOnServer = ctx.Request.Headers;

                            ctx.Response.OnStarting(() =>
                            {
                                ctx.RequestServices.GetRequiredService<ILogger>().LogTrace("server response has begun");

                                _ = Interlocked.Increment(ref host.serverResponseHasBegunCount);

                                return Task.CompletedTask;
                            });

                            try
                            {
                                await next();
                            }
                            finally
                            {
                                _ = Interlocked.Increment(ref host.serverResponseHasFinishedCount);
                            }
                        }
                    )
                    .Use(async (ctx, next) =>
                        {
                            try
                            {
                                await next();
                            }
                            catch (Exception ex)
                            {
                                ctx.RequestServices.GetRequiredService<ILogger>()
                                    .LogError(ex, "exception in request pipeline");

                                if (ctx.Response.HasStarted)
                                {
                                    return;
                                }

                                ctx.Response.StatusCode = 500;
                                await ctx.Response.WriteAsync($"internal server error\n{ex}", CancellationToken.None);
                            }
                        }
                    )
                    .Use(async (ctx, next) =>
                        {
                            if (host.serverCancellationToken is null)
                            {
                                await next();

                                return;
                            }

                            var logger = ctx.RequestServices.GetRequiredService<ILogger>();

                            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                                ctx.RequestAborted,
                                host.serverCancellationToken.Value
                            );

                            ctx.RequestAborted = cts.Token;

                            ctx.RequestAborted.ThrowIfCancellationRequested();

                            await using var d = ctx.RequestAborted.Register(
                                static l => ((ILogger)l!).LogInformation("request aborted"),
                                logger
                            );

                            await next();
                        }
                    )
                    .Use(async (ctx, next) =>
                        {
                            if (host.ServerConnectionResponses.TryDequeue(out var res) && res.HasValue)
                            {
                                ctx.Response.ContentType = res.Value.ContentType;
                                ctx.Response.StatusCode = res.Value.StatusCode;
                                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);

                                if (res.Value.KeepAlive)
                                {
                                    try
                                    {
                                        await Task.Delay(
                                            TimeSpan.FromMinutes(value: 1),
                                            TimeProvider.System,
                                            ctx.RequestAborted
                                        );
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        // nothing to do
                                    }
                                }

                                return;
                            }

                            await next();
                        }
                    );

                _ = app.UseConquerorWellKnownErrorHandling();
                _ = app.UseRouting();

                _ = app.UseEndpoints(endpoints =>
                {
                    endpoints
                        .MapMethods("debug/{param:int}", ["GET"], (int param, HttpContext _) => TypedResults.Ok(param))
                        .Finally(e =>
                        {
                            // to allow stepping in with debugger
                            _ = e;
                        });

                    _ = testCase.TransportType switch
                    {
                        HttpSignalConformityTestCase.HttpSignalTransportType.Sse =>
                            endpoints.MapServerSentEventsSignalsEndpoint(
                                HttpSignalTransportConformityTestHost.SseAddress.AbsolutePath
                            ),
                        HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets =>
                            endpoints.MapWebSocketsSignalsEndpoint(
                                HttpSignalTransportConformityTestHost.WebSocketsAddress.AbsolutePath
                            ),
                        _ => throw new InvalidOperationException($"unknown transport type: {testCase.TransportType}"),
                    };
                });
            }
        );

        host.serverCancellationToken = host.serverCts.Token;

        return host;
    }

    public T Resolve<T>()
        where T : notnull => HttpTransportTestHost.Resolve<T>();

    public Task<WebSocket> ConnectToWebSocket(Uri address, Action<IHeaderDictionary>? configureHeaders = null) =>
        HttpTransportTestHost.ConnectToWebSocket(address, configureHeaders);

    public async Task TriggerReconnect()
    {
        ServerResponseHasBegunCount = 0;
        serverCancellationToken = null;

        // this should trigger reconnections
        await serverCts.CancelAsync();
    }
}
