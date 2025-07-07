using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.FileSystem.Tests.Messaging;

public sealed class FileSystemMessageConformityExecutionErrorTestCase
    : FileSystemMessageConformityExecutionTestCase,
      IMessageTransportConformityExecutionErrorTestCase<FileSystemMessageTransportConformityTestHost>
{
    public Exception? ReceiverConfigurationException => ConfigurationExceptions.OfType<Exception>().FirstOrDefault();

    public required IReadOnlyCollection<Exception?> ConfigurationExceptions { get; init; }

    public required Exception? SendException { get; init; }

    public required IReadOnlyCollection<Exception?> HandlerExceptions { get; init; }

    public int? NumOfExpectedUnrecoverableConnectionErrors { get; init; }

    public override FileSystemMessageTransportConformityTestHost CreateTestHost()
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

    public override void ConfigureReceiver(FileSystemMessageTransportConformityTestHost host, IFileSystemMessageReceiver receiver)
    {
        if (host.ReceiverConfigurationExceptions.TryDequeue(out var ex) && ex is not null)
        {
            throw ex;
        }

        base.ConfigureReceiver(host, receiver);
    }
}
