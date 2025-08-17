namespace Conqueror.Transport.FileSystem.Tests.Signalling;

public abstract class FileSystemSignalConformityTestCase
    : ISignalTransportConformityTestCase<FileSystemSignalTransportConformityTestHost>
{
    public required Action<IServiceCollection> RegisterHandler { get; init; }

    public Action<IServiceCollection>? RegisterOnPublisher { get; init; }

    public required Func<ISignalReceivers, CancellationToken, ReceiverExecutionHandle> RunReceivers { get; init; }

    public required Func<ISignalPublishers, CancellationToken, Task> PublishSignals { get; init; }
    public required string Name { get; init; }

    public virtual FileSystemSignalTransportConformityTestHost CreateTestHost() =>
        FileSystemSignalTransportConformityTestHost.Create(this);

    Task ISignalTransportConformityTestCase<FileSystemSignalTransportConformityTestHost>.PublishSignals(
        ISignalPublishers publishers,
        CancellationToken cancellationToken
    ) => PublishSignals(publishers, cancellationToken);

    public virtual void RegisterServerServices(IServiceCollection services) { }

    public virtual void RegisterClientServices(IServiceCollection services) { }

    public virtual void ConfigureReceiver(
        FileSystemSignalTransportConformityTestHost host,
        IFileSystemSignalReceiver receiver
    )
    {
    }
}
