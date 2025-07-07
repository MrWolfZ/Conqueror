using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.FileSystem.Tests.Messaging;

public abstract class FileSystemMessageConformityTestCase : IMessageTransportConformityTestCase<FileSystemMessageTransportConformityTestHost>
{
    public required string Name { get; init; }

    public required Action<IServiceCollection> RegisterHandler { get; init; }

    public Action<IServiceCollection>? RegisterOnSender { get; init; }

    public required Func<IMessageReceivers, CancellationToken, ReceiverExecutionHandle?> RunReceivers { get; init; }

    public required Func<IMessageSenders, CancellationToken, Task<IReadOnlyCollection<object>>> SendMessages { get; init; }

    public virtual FileSystemMessageTransportConformityTestHost CreateTestHost() => FileSystemMessageTransportConformityTestHost.Create(this);

    Task<IReadOnlyCollection<object>> IMessageTransportConformityTestCase<FileSystemMessageTransportConformityTestHost>.SendMessages(
        IMessageSenders messageSenders,
        CancellationToken cancellationToken)
        => SendMessages(messageSenders, cancellationToken);

    public virtual void RegisterServerServices(IServiceCollection services)
    {
    }

    public virtual void RegisterClientServices(IServiceCollection services)
    {
        RegisterOnSender?.Invoke(services);
    }

    public virtual void ConfigureReceiver(FileSystemMessageTransportConformityTestHost host, IFileSystemMessageReceiver receiver)
    {
    }
}
