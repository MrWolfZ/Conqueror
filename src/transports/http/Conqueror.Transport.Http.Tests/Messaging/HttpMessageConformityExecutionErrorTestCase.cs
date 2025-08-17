namespace Conqueror.Transport.Http.Tests.Messaging;

public sealed class HttpMessageConformityExecutionErrorTestCase
    : HttpMessageConformityExecutionTestCase,
        IMessageTransportConformityExecutionErrorTestCase<HttpMessageTransportConformityTestHost>
{
    public required IReadOnlyCollection<Exception?> ConfigurationExceptions { get; init; }
    public Exception? ReceiverConfigurationException => ConfigurationExceptions.OfType<Exception>().FirstOrDefault();

    public required Exception? SendException { get; init; }

    public required IReadOnlyCollection<Exception?> HandlerExceptions { get; init; }

    public int? NumOfExpectedUnrecoverableConnectionErrors => null;

    public override HttpMessageTransportConformityTestHost CreateTestHost()
    {
        var host = base.CreateTestHost();

        foreach (var ex in ConfigurationExceptions)
        {
            host.ReceiverConfigurationExceptions.Enqueue(ex);
        }

        return host;
    }

    public override void RegisterServerServices(IServiceCollection services)
    {
        _ = services.AddSingleton(new ConcurrentQueue<Exception?>(HandlerExceptions));

        base.RegisterServerServices(services);
    }

    public override void RegisterClientServices(IServiceCollection services)
    {
        if (SendException is not null)
        {
            _ = services.AddSingleton(SendException);
        }

        base.RegisterClientServices(services);
    }

    public override void ConfigureReceiver(HttpMessageTransportConformityTestHost host, IHttpMessageReceiver receiver)
    {
        if (host.ReceiverConfigurationExceptions.TryDequeue(out var ex) && ex is not null)
        {
            throw ex;
        }

        base.ConfigureReceiver(host, receiver);
    }
}
