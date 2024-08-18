using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Conqueror.Streaming.Transport.Http.Client.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "request, response, and interface types must be public for dynamic type generation to work")]
public sealed class StreamingHttpClientNonTestHostTests
{
    private const string ListenAddress = "http://localhost:59876";

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_StreamsItems()
    {
        var builder = WebApplication.CreateBuilder();

        ConfigureServerServices(builder.Services);

        await using var app = builder.Build();

        Configure(app);

        var appTask = app.RunAsync(ListenAddress);

        using var cts = new CancellationTokenSource();

        if (!Debugger.IsAttached)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(10));
        }

        var serviceCollection = new ServiceCollection();
        ConfigureClientServices(serviceCollection);
        await using var serviceProvider = serviceCollection.BuildServiceProvider();

        var handler = serviceProvider.GetRequiredService<ITestStreamingRequestHandler>();

        var result = await handler.ExecuteRequest(new(10), cts.Token).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EquivalentTo(new[] { 11, 12, 13 }));

        app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

        await appTask;
    }

    private void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddControllers().AddConquerorStreamingHttpControllers();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();
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

        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b => b.UseWebSocket(new UriBuilder(ListenAddress) { Scheme = "ws" }.Uri));
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

    public interface ITestStreamingRequestHandler : IStreamingRequestHandler<TestRequest, TestItem>;

    private sealed class TestStreamingRequestHandler : ITestStreamingRequestHandler
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }
}
