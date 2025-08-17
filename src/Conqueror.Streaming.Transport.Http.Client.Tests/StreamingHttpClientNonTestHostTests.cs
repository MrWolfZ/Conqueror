namespace Conqueror.Streaming.Transport.Http.Client.Tests;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public sealed class StreamingHttpClientNonTestHostTests
{
    private static readonly Uri ListenAddress = new("http://localhost:59876", UriKind.Absolute);

    [Test]
    [Retry(tryCount: 3)]
    public async Task GivenSuccessfulWebSocketConnection_StreamsItems()
    {
        await using var app = CreateWebApp();
        await using var d = RunWebApp(app);

        await using var serviceProvider = CreateClientSideServiceProvider();
        using var cts = CreateCancellationTokenSource();

        var producer = serviceProvider.GetRequiredService<ITestStreamProducer>();

        var result = await producer.ExecuteRequest(new(Payload: 10), cts.Token).Drain(CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EquivalentTo(new[] { 11, 12, 13 }));
    }

    [Test]
    [Retry(tryCount: 3)]
    public async Task GivenSuccessfulWebSocketConnection_WhenClientCancelsEnumeration_CancellationIsPropagatedToServer()
    {
        await using var app = CreateWebApp();
        await using var d = RunWebApp(app);

        await using var serviceProvider = CreateClientSideServiceProvider();
        using var cts = CreateCancellationTokenSource();

        var producer = serviceProvider.GetRequiredService<ITestStreamProducer>();

        var enumerator = producer.ExecuteRequest(new(Payload: 10), cts.Token).GetAsyncEnumerator(cts.Token);

        _ = await enumerator.MoveNextAsync();
        _ = await enumerator.MoveNextAsync();

        await cts.CancelAsync();

        var observations = app.Services.GetRequiredService<TestObservations>();

        Assert.That(
            () => observations.CancellationWasRequested,
            Is.True.After(Environment.GetEnvironmentVariable("GITHUB_ACTION") is null ? 1 : 10)
                .Seconds.PollEvery(milliSeconds: 100)
                .MilliSeconds
        );
    }

    [Test]
    [Retry(tryCount: 3)]
    public async Task GivenSuccessfulWebSocketConnection_WhenExceptionOccursOnServer_ErrorIsPropagatedToClient()
    {
        await using var app = CreateWebApp();
        await using var d = RunWebApp(app);

        await using var serviceProvider = CreateClientSideServiceProvider();
        using var cts = CreateCancellationTokenSource();

        var producer = serviceProvider.GetRequiredService<ITestStreamProducer>();

        var p = app.Services.GetRequiredService<TestParams>();

        p.ExceptionToThrow = new Exception("Test exception");

        var ex = Assert.ThrowsAsync<HttpStreamFailedException>(() =>
            producer.ExecuteRequest(new(Payload: 10), cts.Token).Drain(CancellationToken.None)
        );
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
        var appTask = app.RunAsync(ListenAddress.AbsoluteUri);

        return new AnonymousAsyncDisposable(() =>
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
            cts.CancelAfter(TimeSpan.FromSeconds(value: 10));
        }

        return cts;
    }

    private void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddControllers().AddConquerorStreamingHttpControllers();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();
        _ = services.AddSingleton<TestObservations>().AddSingleton<TestParams>();
    }

    private void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorStreamingHttpClientServices(o =>
        {
            o.JsonSerializerOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        });

        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(b =>
            b.UseWebSocket(new UriBuilder(ListenAddress) { Scheme = "ws" }.Uri)
        );
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

    // ReSharper disable once ClassNeverInstantiated.Local - used by reflection
    private sealed class TestStreamProducer(TestObservations observations, TestParams p) : ITestStreamProducer
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(
            TestRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var d = cancellationToken.Register(() => observations.CancellationWasRequested = true);

            await Task.Yield();

            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);

            if (p.ExceptionToThrow is not null)
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
        public async ValueTask DisposeAsync() => await dispose();
    }
}
