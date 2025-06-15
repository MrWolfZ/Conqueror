using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.Http.Tests.Messaging;

public delegate Task FnToCallFromHandler(object message, CancellationToken cancellationToken);

public sealed class HttpMessageTransportConformityReceiverTestHost : IMessageTransportConformityReceiverTestHost
{
    private readonly Action onDisposeOrCancel;

    private HttpMessageTransportConformityReceiverTestHost(Action onDisposeOrCancel)
    {
        this.onDisposeOrCancel = onDisposeOrCancel;
    }

    private HttpTransportTestHost HttpTransportTestHost { get; set; } = null!;

    public ConcurrentQueue<IHeaderDictionary> ReceivedHeadersOnServer { get; } = [];

    public HttpClient HttpClient => HttpTransportTestHost.HttpClient;

    public ReceiverExecutionHandle? ReceiverExecutionHandle => null;

    public static async Task<HttpMessageTransportConformityReceiverTestHost> CreateReceiverHost(
        HttpMessageTransportConformityTestHost host,
        HttpMessageConformityTestCase testCase,
        Action<IServiceCollection>? configureServices,
        Action<IApplicationBuilder>? configure,
        Func<object, ConquerorContext, CancellationToken, Task>? messageCallback,
        Action onDisposeOrCancel,
        CancellationToken cancellationToken)
    {
        var reg = cancellationToken.Register(onDisposeOrCancel);

        var receiverHost = new HttpMessageTransportConformityReceiverTestHost(() =>
        {
            reg.Dispose();
            onDisposeOrCancel();
        });

        receiverHost.HttpTransportTestHost = await HttpTransportTestHost.Create(
            services =>
            {
                _ = services.AddConquerorHttpServerAspNetCore()
                            .AddRouting()
                            .AddSingleton(ILogger (p) => p.GetRequiredService<ILogger<HttpMessageTransportConformityTestHost>>())
                            .AddSingleton<Action<IHttpMessageReceiver>>(r => testCase.ConfigureReceiver(host, r))
                            .AddSingleton<FnToCallFromHandler>(p => (s, ct) => messageCallback?.Invoke(
                                                                                    s,
                                                                                    p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                                                                    ct)
                                                                                ?? Task.CompletedTask);

                testCase.RegisterHandler(services);

                testCase.RegisterServerServices(services);

                configureServices?.Invoke(services);
            },
            app =>
            {
                _ = app.Use(async (ctx, next) =>
                       {
                           receiverHost.ReceivedHeadersOnServer.Enqueue(ctx.Request.Headers);
                           await next();
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
                               await ctx.Response.WriteAsync($"internal server error\n{ex}", ctx.RequestAborted);
                           }
                       });

                configure?.Invoke(app);

                _ = app.UseConquerorWellKnownErrorHandling();
                _ = app.UseRouting();

                _ = app.UseEndpoints(endpoints =>
                {
                    endpoints.MapMethods("debug/{param:int}", ["GET"], (int param, HttpContext _) => TypedResults.Ok(param))
                             .Finally(e =>
                             {
                                 // to allow stepping in with debugger
                                 _ = e;
                             });

                    testCase.MapEndpoints(endpoints);
                });
            });

        return receiverHost;
    }

    public T Resolve<T>()
        where T : notnull
        => HttpTransportTestHost.Resolve<T>();

    public async ValueTask DisposeAsync()
    {
        onDisposeOrCancel();

        await HttpTransportTestHost.DisposeAsync();
    }
}
