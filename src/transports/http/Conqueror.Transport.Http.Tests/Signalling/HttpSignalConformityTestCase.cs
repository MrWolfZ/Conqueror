using System.Net.WebSockets;
using Conqueror.Transports.ConformityTests.Signalling;

namespace Conqueror.Transport.Http.Tests.Signalling;

public delegate Task FnToCallFromHandler(object signal, CancellationToken cancellationToken);

public abstract class HttpSignalConformityTestCase : ISignalTransportConformityTestCase<HttpSignalTransportConformityTestHost>
{
    public enum HttpSignalTransportType
    {
        Sse,
        WebSockets,
    }

    public static readonly Uri SseAddress = new("http://localhost/api/signals/sse");
    public static readonly Uri WebSocketsAddress = new("ws://localhost/api/signals/ws");

    public required string Name { get; init; }

    public required HttpSignalTransportType TransportType { get; init; }

    public required Action<IServiceCollection> RegisterHandler { get; init; }

    public Action<IServiceCollection>? RegisterOnServer { get; init; }

    public required Func<ISignalReceivers, CancellationToken, SignalReceiverRun> RunReceivers { get; init; }

    public required Func<ISignalPublishers, CancellationToken, Func<object, ConquerorContext, CancellationToken, Task>?, Task> PublishSignals { get; init; }

    public Action<IHeaderDictionary>? ConfigureHeaders { get; init; }

    public virtual async Task<HttpSignalTransportConformityTestHost> CreateTestHost()
    {
        return await HttpSignalTransportConformityTestHost.Create(
            this,
            app =>
            {
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

                    _ = TransportType switch
                    {
                        HttpSignalTransportType.Sse => endpoints.MapServerSentEventsSignalsEndpoint(SseAddress.AbsolutePath),
                        HttpSignalTransportType.WebSockets => endpoints.MapWebSocketsSignalsEndpoint(WebSocketsAddress.AbsolutePath),
                        _ => throw new ArgumentOutOfRangeException(nameof(TransportType), TransportType, null),
                    };
                });
            });
    }

    SignalReceiverRun ISignalTransportConformityTestCase<HttpSignalTransportConformityTestHost>.RunReceivers(
        ISignalReceivers receivers,
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? signalCallback,
        Func<CancellationToken, Task>? reconnectDelayCallback)
    {
        var receiverConfiguration = receivers.ServiceProvider.GetRequiredService<ReceiverConfiguration>();
        receiverConfiguration.SignalCallback = signalCallback;
        receiverConfiguration.ReconnectDelayCallback = reconnectDelayCallback;

        return RunReceivers(receivers, cancellationToken);
    }

    Task ISignalTransportConformityTestCase<HttpSignalTransportConformityTestHost>.PublishSignals(
        ISignalPublishers publishers,
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? publishCallback)
        => PublishSignals(publishers, cancellationToken, publishCallback);

    public virtual void RegisterServerServices(IServiceCollection services)
    {
    }

    public virtual void RegisterClientServices(IServiceCollection services)
    {
        _ = services.AddSingleton<ReceiverConfiguration>()
                    .AddSingleton<FnToCallFromHandler>(p => (s, ct) => p.GetRequiredService<ReceiverConfiguration>()
                                                                        .SignalCallback?
                                                                        .Invoke(s, p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, ct)
                                                                       ?? Task.CompletedTask);
    }

    public virtual void ConfigureSseReceiver(HttpSignalTransportConformityTestHost host, IHttpSseSignalReceiver receiver)
    {
        var config = receiver.Enable(SseAddress)
                             .WithHttpClient(host.HttpClient)
                             .WithHeaders(h =>
                             {
                                 var headerDictionary = new HeaderDictionary();
                                 ConfigureHeaders?.Invoke(headerDictionary);

                                 foreach (var (key, value) in headerDictionary)
                                 {
                                     foreach (var valuePart in value)
                                     {
                                         h.Add(key, valuePart);
                                     }
                                 }
                             })
                             .WithSignalCallback(s => host.Logger.LogInformation("signal callback: {Signal}", s))
                             .WithExceptionCallback(ex => host.Logger.LogError(ex, "exception callback"));

        var receiverConfiguration = receiver.ServiceProvider.GetRequiredService<ReceiverConfiguration>();

        if (receiverConfiguration.ReconnectDelayCallback != null)
        {
            var expectedStatusCodes = new Queue<int>([StatusCodes.Status503ServiceUnavailable, StatusCodes.Status200OK]);

            _ = config

                // test that function can be overwritten
                .WithReconnectDelayFunction((_, _) => throw new NotSupportedException())
                .WithReconnectDelayFunction(async (statusCode, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    Assert.That(
                        statusCode,
                        Is.EqualTo(expectedStatusCodes.Dequeue()));

                    await receiverConfiguration.ReconnectDelayCallback(ct);
                });
        }
    }

    public virtual void ConfigureWebSocketsReceiver(HttpSignalTransportConformityTestHost host, IHttpWebSocketsSignalReceiver receiver)
    {
        var config = receiver.Enable(WebSocketsAddress)
                             .WithWebSocketFactory((address, _) => host.ConnectToWebSocket(address, ConfigureHeaders))
                             .WithSignalCallback(s => host.Logger.LogInformation("signal callback: {Signal}", s))
                             .WithExceptionCallback(ex => host.Logger.LogError(ex, "exception callback"));

        var receiverConfiguration = receiver.ServiceProvider.GetRequiredService<ReceiverConfiguration>();

        if (receiverConfiguration.ReconnectDelayCallback != null)
        {
            var expectedStatusCodes = new Queue<int>([StatusCodes.Status503ServiceUnavailable, StatusCodes.Status200OK]);

            _ = config

                // test that function can be overwritten
                .WithReconnectDelayFunction((
                                                    _,
                                                    _,
                                                    _,
                                                    _)
                                                => throw new NotSupportedException())
                .WithReconnectDelayFunction(async (
                                                closeStatus,
                                                statusCode,
                                                _,
                                                ct) =>
                                            {
                                                ct.ThrowIfCancellationRequested();

                                                if (expectedStatusCodes.TryDequeue(out var expectedStatusCode))
                                                {
                                                    Assert.That(statusCode, Is.EqualTo(expectedStatusCode));

                                                    // the TestWebSocket returns no close status in the error case
                                                    Assert.That(
                                                        closeStatus,
                                                        Is.EqualTo(
                                                            statusCode is StatusCodes.Status200OK
                                                                ? WebSocketCloseStatus.NormalClosure
                                                                : WebSocketCloseStatus.Empty));
                                                }
                                                else
                                                {
                                                    Assert.That(closeStatus, Is.EqualTo(WebSocketCloseStatus.NormalClosure));
                                                }

                                                await receiverConfiguration.ReconnectDelayCallback(ct);
                                            });
        }
    }

    private sealed class ReceiverConfiguration
    {
        public Func<object, ConquerorContext, CancellationToken, Task>? SignalCallback { get; set; }

        public Func<CancellationToken, Task>? ReconnectDelayCallback { get; set; }
    }
}
