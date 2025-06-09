using System.Collections.Concurrent;
using System.Net.WebSockets;
using Conqueror.Transports.ConformityTests.Signalling;

namespace Conqueror.Transport.Http.Tests.Signalling;

public sealed class HttpSignalTransportConformityTestHost : ISignalTransportConformityTestHost<HttpSignalTransportConformityTestHost>
{
    private readonly CancellationTokenSource serverCts = new();

    private int serverCallCount;
    private CancellationToken? serverCancellationToken;
    private int serverResponseHasBegunCount;
    private int serverResponseHasFinishedCount;

    private HttpSignalTransportConformityTestHost()
    {
    }

    private HttpTransportTestHost HttpTransportTestHost { get; set; } = null!;

    private ServiceProvider ClientServiceProvider { get; set; } = null!;

    public HttpClient HttpClient => HttpTransportTestHost.HttpClient;

    public CancellationToken TestTimeoutToken => HttpTransportTestHost.TestTimeoutToken;

    public TimeSpan AssertionTimeout => HttpTransportTestHost.AssertionTimeout;

    public int AssertionTimeoutInMs => HttpTransportTestHost.AssertionTimeoutInMs;

    public ILogger Logger => HttpTransportTestHost.Resolve<ILogger<HttpSignalTransportConformityTestHost>>();

    public ISignalReceivers SignalReceivers => ClientServiceProvider.GetRequiredService<ISignalReceivers>();

    public ISignalPublishers SignalPublishers => HttpTransportTestHost.Resolve<ISignalPublishers>();

    public IConquerorContextAccessor PublisherConquerorContextAccessor => HttpTransportTestHost.Resolve<IConquerorContextAccessor>();

    public int ServerCallCount => serverCallCount;

    public int ServerResponseHasBegunCount
    {
        get => serverResponseHasBegunCount;
        set => serverResponseHasBegunCount = value;
    }

    public int ServerResponseHasFinishedCount => serverResponseHasFinishedCount;

    public Queue<Exception?> ReceiverConfigurationExceptions { get; } = new();

    public IHeaderDictionary? ReceivedHeadersOnServer { get; private set; }

    public ConcurrentQueue<(int StatusCode, string ContentType, bool KeepAlive)?> ServerConnectionResponses { get; } = [];

    public static async Task<HttpSignalTransportConformityTestHost> Create(
        HttpSignalConformityTestCase testCase,
        Action<IApplicationBuilder> configure)
    {
        var host = new HttpSignalTransportConformityTestHost();

        host.HttpTransportTestHost = await HttpTransportTestHost.Create(
            services =>
            {
                testCase.RegisterOnServer?.Invoke(services);

                _ = services.AddConquerorHttpServerAspNetCore()
                            .AddRouting()
                            .AddSingleton(ILogger (p) => p.GetRequiredService<ILogger<HttpSignalTransportConformityTestHost>>());

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
                               ctx.RequestServices
                                  .GetRequiredService<ILogger>()
                                  .LogTrace("server response has begun");

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
                       })
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
                               await ctx.Response.WriteAsync($"internal server error\n{ex}");
                           }
                       })
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
                               host.serverCancellationToken.Value);

                           ctx.RequestAborted = cts.Token;

                           ctx.RequestAborted.ThrowIfCancellationRequested();

                           await using var d = ctx.RequestAborted.Register(static l => ((ILogger)l!).LogInformation("request aborted"), logger);

                           await next();
                       })
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
                                       await Task.Delay(TimeSpan.FromMinutes(1), ctx.RequestAborted);
                                   }
                                   catch (OperationCanceledException)
                                   {
                                       // nothing to do
                                   }
                               }

                               return;
                           }

                           await next();
                       });

                configure(app);
            });

        var services = new ServiceCollection()
                       .AddConquerorHttpClient()
                       .AddSingleton<Action<IHttpSseSignalReceiver>>(p => testCase.ConfigureSseReceiver(host, p))
                       .AddSingleton<Action<IHttpWebSocketsSignalReceiver>>(p => testCase.ConfigureWebSocketsReceiver(host, p))
                       .AddSingleton(host)
                       .AddSingleton(host.Logger)
                       .AddTransient(typeof(HttpSignalTestCases.TestSignalMiddleware<>));

        testCase.RegisterHandler(services);

        testCase.RegisterClientServices(services);

        host.ClientServiceProvider = services.BuildServiceProvider();

        host.serverCancellationToken = host.serverCts.Token;

        return host;
    }

    public T ResolveOnServer<T>()
        where T : notnull
        => HttpTransportTestHost.Resolve<T>();

    public T ResolveOnClient<T>()
        where T : notnull
        => ClientServiceProvider.GetRequiredService<T>();

    public Task<WebSocket> ConnectToWebSocket(Uri address, Action<IHeaderDictionary>? configureHeaders = null)
        => HttpTransportTestHost.ConnectToWebSocket(address, configureHeaders);

    public async Task TriggerReconnect()
    {
        ServerResponseHasBegunCount = 0;
        serverCancellationToken = null;

        // this should trigger reconnections
        await serverCts.CancelAsync();
    }

    public async ValueTask DisposeAsync()
    {
        serverCts.Dispose();
        await ClientServiceProvider.DisposeAsync();
        await HttpTransportTestHost.DisposeAsync();
    }
}
