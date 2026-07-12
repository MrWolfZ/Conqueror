namespace Examples.BlazorWebAssembly.Tests;

using System.Diagnostics;
using API;
using Conqueror;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class AppTestHost : IStartupFilter, IAsyncDisposable
{
    private AppTestHost(Action<IServiceCollection>? configureServices)
    {
        ApplicationFactory = new WebApplicationFactory<Program>();

        ApplicationFactory = ApplicationFactory.WithWebHostBuilder(builder =>
        {
            _ = builder.ConfigureServices(services =>
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter>(this));

                configureServices?.Invoke(services);
            });
        });

        HttpClient = ApplicationFactory.CreateClient();

        ClientServiceProvider = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();
    }

    public CancellationToken TimeoutToken => CancellationTokenSource.Token;

    public DateTimeOffset CurrentTime =>
        new(year: 2025, month: 1, day: 1, hour: 12, minute: 0, second: 0, TimeSpan.Zero);

    private WebApplicationFactory<Program> ApplicationFactory { get; }

    private HttpClient HttpClient { get; }

    private ServiceProvider ClientServiceProvider { get; }

    private CancellationTokenSource CancellationTokenSource { get; } = new();

    public async ValueTask DisposeAsync()
    {
        await ApplicationFactory.DisposeAsync();
        await CastAndDispose(HttpClient);
        await CastAndDispose(CancellationTokenSource);
        await CastAndDispose(ClientServiceProvider);

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

    Action<IApplicationBuilder> IStartupFilter.Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            _ = app.Use(async (_, n) =>
                {
                    using var t = SystemTime.WithCurrentTime(CurrentTime);
                    await n();
                }
            );

            next(app);
        };
    }

    public TIHandler CreateMessageHttpSender<TMessage, TResponse, TIHandler>(
        MessageTypes<TMessage, TResponse, TIHandler> _
    )
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler> =>
        ClientServiceProvider
            .GetRequiredService<IMessageSenders>()
            .For(_)
            .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(HttpClient));

    public static AppTestHost Create(Action<IServiceCollection>? configureServices = null)
    {
        var host = new AppTestHost(configureServices);

        if (!Debugger.IsAttached)
        {
            host.CancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(value: 2));
        }

        return host;
    }

    public T Resolve<T>()
        where T : notnull => ApplicationFactory.Services.GetRequiredService<T>();
}
