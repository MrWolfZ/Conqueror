using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Conqueror.Streaming.Transport.Http.Client.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "request, response, and interface types must be public for dynamic type generation to work")]
public sealed class StreamingHttpClientNonTestHostTests
{
    private const string ListenAddress = "http://localhost:59876";

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_StreamsItems()
    {
        await using var app = CreateWebApp();
        await using var d = RunWebApp(app);

        await using var serviceProvider = CreateClientSideServiceProvider();
        using var cts = CreateCancellationTokenSource();

        var producer = serviceProvider.GetRequiredService<ITestStreamProducer>();

        var result = await producer.ExecuteRequest(new(10), cts.Token).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EquivalentTo(new[] { 11, 12, 13 }));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_WhenClientCancelsEnumeration_CancellationIsPropagatedToServer()
    {
        await using var app = CreateWebApp();
        await using var d = RunWebApp(app);

        await using var serviceProvider = CreateClientSideServiceProvider();
        using var cts = CreateCancellationTokenSource();

        var producer = serviceProvider.GetRequiredService<ITestStreamProducer>();

        var enumerator = producer.ExecuteRequest(new(10), cts.Token).GetAsyncEnumerator(cts.Token);

        _ = await enumerator.MoveNextAsync();
        _ = await enumerator.MoveNextAsync();

        await cts.CancelAsync();

        var observations = app.Services.GetRequiredService<TestObservations>();

        Assert.That(() => observations.CancellationWasRequested, Is.True.After(1).Seconds.PollEvery(100).MilliSeconds);
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_WhenExceptionOccursOnServer_ErrorIsPropagatedToClient()
    {
        await using var app = CreateWebApp();
        await using var d = RunWebApp(app);

        await using var serviceProvider = CreateClientSideServiceProvider();
        using var cts = CreateCancellationTokenSource();

        var producer = serviceProvider.GetRequiredService<ITestStreamProducer>();

        var p = app.Services.GetRequiredService<TestParams>();

        p.ExceptionToThrow = new("Test exception");

        var ex = Assert.ThrowsAsync<HttpStreamFailedException>(() => producer.ExecuteRequest(new(10), cts.Token).Drain());
        Assert.That(ex.Message, Is.EqualTo(p.ExceptionToThrow.Message));
    }

    private WebApplication CreateWebApp()
    {
        var builder = WebApplication.CreateBuilder();

        ConfigureServerServices(builder.Services);

        var app = builder.Build();

        Configure(app);

        return app;
    }

    private static AnonymousAsyncDisposable RunWebApp(WebApplication app)
    {
        var appTask = app.RunAsync(ListenAddress);

        return new(() =>
        {
            app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            return appTask;
        });
    }

    private ServiceProvider CreateClientSideServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureClientServices(serviceCollection);
        return serviceCollection.BuildServiceProvider();
    }

    private CancellationTokenSource CreateCancellationTokenSource()
    {
        var cts = new CancellationTokenSource();

        if (!Debugger.IsAttached)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(10));
        }

        return cts;
    }

    private void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddControllers().AddConquerorStreamingHttpControllers();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();
        _ = services.AddSingleton<TestObservations>()
                    .AddSingleton<TestParams>();
    }

    private void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorStreamingHttpClientServices(o =>
        {
            o.JsonSerializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
            };
        });

        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(b => b.UseWebSocket(new UriBuilder(ListenAddress) { Scheme = "ws" }.Uri));
    }

    private void Configure(IApplicationBuilder app)
    {
        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseWebSockets();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    [HttpStream]
    public sealed record TestRequest(int Payload);

    public sealed record TestItem(int Payload);

    public interface ITestStreamProducer : IStreamProducer<TestRequest, TestItem>;

    private sealed class TestStreamProducer(TestObservations observations, TestParams p) : ITestStreamProducer
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var d = cancellationToken.Register(() => observations.CancellationWasRequested = true);

            await Task.Yield();
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);

            if (p.ExceptionToThrow != null)
            {
                throw p.ExceptionToThrow;
            }
        }
    }

    private sealed class TestObservations
    {
        public bool CancellationWasRequested { get; set; }
    }

    private sealed class TestParams
    {
        public Exception? ExceptionToThrow { get; set; }
    }

    private sealed class AnonymousAsyncDisposable(Func<Task> dispose) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await dispose();
        }
    }
}
