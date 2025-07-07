namespace Conqueror.Transport.FileSystem.Tests.Signalling;

public sealed class FileSystemSignalConformityExecutionErrorTestCase
    : FileSystemSignalConformityExecutionTestCase,
      ISignalTransportConformityExecutionErrorTestCase<FileSystemSignalTransportConformityTestHost>
{
    public Exception? ReceiverConfigurationException => ConfigurationExceptions.OfType<Exception>().FirstOrDefault();

    public required IReadOnlyCollection<Exception?> ConfigurationExceptions { get; init; }

    public required Exception? PublishException { get; init; }

    public required IReadOnlyCollection<Exception?> HandlerExceptions { get; init; }

    public int? NumOfExpectedUnrecoverableConnectionErrors { get; init; }

    public override FileSystemSignalTransportConformityTestHost CreateTestHost()
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

    public override void ConfigureReceiver(FileSystemSignalTransportConformityTestHost host, IFileSystemSignalReceiver receiver)
    {
        if (host.ReceiverConfigurationExceptions.TryDequeue(out var ex) && ex is not null)
        {
            throw ex;
        }

        base.ConfigureReceiver(host, receiver);
    }
}
