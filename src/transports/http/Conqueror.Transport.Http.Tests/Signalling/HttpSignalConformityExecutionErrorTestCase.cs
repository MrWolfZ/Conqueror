namespace Conqueror.Transport.Http.Tests.Signalling;

public sealed class HttpSignalConformityExecutionErrorTestCase : HttpSignalConformityExecutionTestCase,
                                                        ISignalTransportConformityExecutionErrorTestCase<HttpSignalTransportConformityTestHost>
{
    public Exception? ConfigurationException => ConfigurationExceptions.OfType<Exception>().FirstOrDefault();

    public required IReadOnlyCollection<Exception?> ConfigurationExceptions { get; init; }

    public required Exception? PublishException { get; init; }

    public required IReadOnlyCollection<Exception?> HandlerExceptions { get; init; }

    public int NumOfExpectedUnrecoverableConnectionErrors => ConnectionResponses
        .Count(r => r is not null
                    && (r.Value.StatusCode is >= 400 and < 500
                        || (TransportType == HttpSignalTransportType.Sse
                            && r.Value.StatusCode is >= 200 and < 400
                            && r.Value.ContentType != ContentTypes.EventStream)));

    public required IReadOnlyCollection<(int StatusCode, string ContentType, bool KeepAlive)?> ConnectionResponses { get; init; }

    public required int ExpectedInitialConnectionCount { get; init; }

    public override async Task<HttpSignalTransportConformityTestHost> CreateTestHost()
    {
        var host = await base.CreateTestHost();

        foreach (var statusCode in ConnectionResponses)
        {
            host.ServerConnectionResponses.Enqueue(statusCode);
        }

        foreach (var exception in ConfigurationExceptions)
        {
            host.ReceiverConfigurationExceptions.Enqueue(exception);
        }

        return host;
    }

    public override void RegisterServerServices(IServiceCollection services)
    {
        if (PublishException is not null)
        {
            _ = services.AddSingleton(PublishException);
        }

        base.RegisterServerServices(services);
    }

    public override void RegisterClientServices(IServiceCollection services)
    {
        _ = services.AddSingleton(new ConcurrentQueue<Exception?>(HandlerExceptions));

        base.RegisterClientServices(services);
    }

    public override void ConfigureWebSocketsReceiver(HttpSignalTransportConformityTestHost host, IHttpWebSocketsSignalReceiver receiver)
    {
        if (host.ReceiverConfigurationExceptions.TryDequeue(out var ex) && ex is not null)
        {
            throw ex;
        }

        base.ConfigureWebSocketsReceiver(host, receiver);
    }

    public override void ConfigureSseReceiver(HttpSignalTransportConformityTestHost host, IHttpSseSignalReceiver receiver)
    {
        if (host.ReceiverConfigurationExceptions.TryDequeue(out var ex) && ex is not null)
        {
            throw ex;
        }

        base.ConfigureSseReceiver(host, receiver);
    }

    public Task OnConfigurationException(HttpSignalTransportConformityTestHost testHost)
    {
        Assert.That(testHost.ServerCallCount, Is.EqualTo(0));

        return Task.CompletedTask;
    }

    public async Task OnInitialConnection(HttpSignalTransportConformityTestHost testHost)
    {
        Assert.That(
            () => testHost.ServerCallCount,
            Is.EqualTo(ExpectedInitialConnectionCount)
              .After(testHost.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await Task.Delay(10, testHost.TestTimeoutToken);

        if (NumOfExpectedUnrecoverableConnectionErrors > 0)
        {
            Assert.That(
                () => testHost.ServerResponseHasFinishedCount,
                Is.EqualTo(NumOfReceivers)
                  .After(testHost.AssertionTimeoutInMs)
                  .MilliSeconds
                  .PollEvery(10)
                  .MilliSeconds);

            return;
        }

        Assert.That(
            () => testHost.ServerResponseHasBegunCount,
            Is.EqualTo(ExpectedInitialConnectionCount)
              .After(testHost.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);
    }

    public Task OnHandlerExceptions(HttpSignalTransportConformityTestHost testHost)
    {
        Assert.That(
            () => testHost.ServerResponseHasFinishedCount,
            Is.EqualTo(NumOfReceivers)
              .After(testHost.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        return Task.CompletedTask;
    }

    public Task TriggerReconnect(HttpSignalTransportConformityTestHost testHost) => testHost.TriggerReconnect();

    public Task AfterSuccessfulReconnect(HttpSignalTransportConformityTestHost testHost)
    {
        Assert.That(
            () => testHost.ServerResponseHasBegunCount,
            Is.EqualTo(NumOfReceivers)
              .After(testHost.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        return Task.CompletedTask;
    }
}
