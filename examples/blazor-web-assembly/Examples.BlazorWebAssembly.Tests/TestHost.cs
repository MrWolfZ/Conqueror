using System.Diagnostics;
using Conqueror;
using Examples.BlazorWebAssembly.API;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Examples.BlazorWebAssembly.Tests;

public sealed class TestHost : IStartupFilter,
                               IAsyncDisposable
{
    private TestHost(Action<IServiceCollection>? configureServices)
    {
        ApplicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter>(this));

                    configureServices?.Invoke(services);
                });
            });

        HttpClient = ApplicationFactory.CreateClient();

        ClientServiceProvider = new ServiceCollection().AddConqueror().BuildServiceProvider();
    }

    private WebApplicationFactory<Program> ApplicationFactory { get; }

    private HttpClient HttpClient { get; }

    private ServiceProvider ClientServiceProvider { get; }

    private CancellationTokenSource CancellationTokenSource { get; } = new();

    public CancellationToken TimeoutToken => CancellationTokenSource.Token;

    public DateTimeOffset CurrentTime => new(
        2025,
        1,
        1,
        12,
        0,
        0,
        TimeSpan.Zero);

    public TIHandler CreateMessageHttpSender<TMessage, TResponse, TIHandler>(MessageTypes<TMessage, TResponse, TIHandler> _)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        => ClientServiceProvider.GetRequiredService<IMessageSenders>()
                                .For(_)
                                .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(HttpClient));

    public static TestHost Create(Action<IServiceCollection>? configureServices = null)
    {
        var host = new TestHost(configureServices);

        if (!Debugger.IsAttached)
        {
            host.CancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(2));
        }

        return host;
    }

    public T Resolve<T>()
        where T : notnull => ApplicationFactory.Services.GetRequiredService<T>();

    Action<IApplicationBuilder> IStartupFilter.Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (_, n) =>
            {
                using var t = SystemTime.WithCurrentTime(CurrentTime);
                await n();
            });

            next(app);
        };
    }

    public async ValueTask DisposeAsync()
    {
        await ApplicationFactory.DisposeAsync();
        await CastAndDispose(HttpClient);
        await CastAndDispose(CancellationTokenSource);
        await CastAndDispose(ClientServiceProvider);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }
}
