namespace Conqueror.Transport.Http.Tests.Signalling;

public delegate Task FnToCallFromHandler(object signal, CancellationToken cancellationToken);

public sealed class HttpSignalTransportConformityReceiverTestHost : ISignalTransportConformityReceiverTestHost
{
    private readonly Func<HttpSignalTransportConformityReceiverTestHost, Task> onDispose;
    private readonly ServiceProvider serviceProvider;

    private HttpSignalTransportConformityReceiverTestHost(
        ServiceProvider serviceProvider,
        Func<HttpSignalTransportConformityReceiverTestHost, Task> onDispose
    )
    {
        this.serviceProvider = serviceProvider;
        this.onDispose = onDispose;
    }

    public required ReceiverExecutionHandle? ReceiverExecutionHandle { get; init; }

    public async ValueTask DisposeAsync()
    {
        await onDispose(this);

        if (ReceiverExecutionHandle is not null)
        {
            await ReceiverExecutionHandle.DisposeAsync();
        }

        await serviceProvider.DisposeAsync();
    }

    public static HttpSignalTransportConformityReceiverTestHost CreateReceiverHost(
        HttpSignalTransportConformityTestHost host,
        HttpSignalConformityTestCase testCase,
        Func<object, ConquerorContext, CancellationToken, Task>? signalCallback,
        Func<CancellationToken, Task>? reconnectDelayCallback,
        Action<HttpSignalTransportConformityReceiverTestHost> onDisposeOrCancel,
        CancellationToken cancellationToken
    )
    {
        var services = new ServiceCollection();

        _ = services
            .AddConquerorHttpClient()
            .AddSingleton<Action<IHttpSseSignalReceiver>>(r =>
                ConfigureSseReceiver(host, testCase, r, reconnectDelayCallback)
            )
            .AddSingleton<Action<IHttpWebSocketsSignalReceiver>>(r =>
                ConfigureWebSocketsReceiver(host, testCase, r, reconnectDelayCallback)
            )
            .AddSingleton(host.Logger)
            .AddTransient(typeof(HttpSignalTestCases.TestSignalMiddleware<>))
            .AddSingleton<FnToCallFromHandler>(p =>
                (s, ct) =>
                    signalCallback?.Invoke(s, p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, ct)
                    ?? Task.CompletedTask
            );

        testCase.RegisterHandler(services);

        testCase.RegisterClientServices(services);

        var p = services.BuildServiceProvider();

        var executionHandle = testCase.RunReceivers(p.GetRequiredService<ISignalReceivers>(), cancellationToken);

        CancellationTokenRegistration? reg = null;

        var receiverHost = new HttpSignalTransportConformityReceiverTestHost(
            p,
            async h =>
            {
                // ReSharper disable once AccessToModifiedClosure
                if (reg.HasValue)
                {
                    // ReSharper disable once AccessToModifiedClosure
                    await reg.Value.DisposeAsync();
                }

                onDisposeOrCancel(h);
            }
        )
        {
            ReceiverExecutionHandle = executionHandle,
        };

        reg = cancellationToken.Register(
            h => onDisposeOrCancel((HttpSignalTransportConformityReceiverTestHost)h!),
            receiverHost
        );

        return receiverHost;
    }

    public T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();

    private static void ConfigureSseReceiver(
        HttpSignalTransportConformityTestHost host,
        HttpSignalConformityTestCase testCase,
        IHttpSseSignalReceiver receiver,
        Func<CancellationToken, Task>? reconnectDelayCallback
    )
    {
        testCase.ConfigureSseReceiver(host, receiver);

        var config = receiver
            .Enable(HttpSignalTransportConformityTestHost.SseAddress)
            .WithHttpClient(host.PublisherHost.HttpClient)
            .WithHeaders(h =>
            {
                var headerDictionary = new HeaderDictionary();
                testCase.ConfigureHeaders?.Invoke(headerDictionary);

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

        if (reconnectDelayCallback is not null)
        {
            var expectedStatusCodes = new Queue<int>(
                [StatusCodes.Status503ServiceUnavailable, StatusCodes.Status200OK]
            );

            _ = config
                // test that function can be overwritten
                .WithReconnectDelayFunction((_, _) => throw new NotSupportedException())
                .WithReconnectDelayFunction(
                    async (statusCode, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();

                        Assert.That(statusCode, Is.EqualTo(expectedStatusCodes.Dequeue()));

                        await reconnectDelayCallback(ct);
                    }
                );
        }
    }

    private static void ConfigureWebSocketsReceiver(
        HttpSignalTransportConformityTestHost host,
        HttpSignalConformityTestCase testCase,
        IHttpWebSocketsSignalReceiver receiver,
        Func<CancellationToken, Task>? reconnectDelayCallback
    )
    {
        testCase.ConfigureWebSocketsReceiver(host, receiver);

        var config = receiver
            .Enable(HttpSignalTransportConformityTestHost.WebSocketsAddress)
            .WithWebSocketFactory((address, _) => host.ConnectToWebSocket(address, testCase.ConfigureHeaders))
            .WithSignalCallback(s => host.Logger.LogInformation("signal callback: {Signal}", s))
            .WithExceptionCallback(ex => host.Logger.LogError(ex, "exception callback"));

        if (reconnectDelayCallback is not null)
        {
            var expectedStatusCodes = new Queue<int>(
                [StatusCodes.Status503ServiceUnavailable, StatusCodes.Status200OK]
            );

            _ = config
                // test that function can be overwritten
                .WithReconnectDelayFunction((_, _, _, _) => throw new NotSupportedException())
                .WithReconnectDelayFunction(
                    async (closeStatus, statusCode, _, ct) =>
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
                                        : WebSocketCloseStatus.Empty
                                )
                            );
                        }
                        else
                        {
                            Assert.That(closeStatus, Is.EqualTo(WebSocketCloseStatus.NormalClosure));
                        }

                        await reconnectDelayCallback(ct);
                    }
                );
        }
    }
}
