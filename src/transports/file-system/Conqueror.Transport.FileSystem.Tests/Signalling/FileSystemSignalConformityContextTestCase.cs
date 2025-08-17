namespace Conqueror.Transport.FileSystem.Tests.Signalling;

public sealed class FileSystemSignalConformityContextTestCase
    : FileSystemSignalConformityTestCase,
      ISignalTransportConformityContextTestCase<FileSystemSignalTransportConformityTestHost>
{
    public Func<FileSystemSignalTransportConformityTestHost, Task>? BeforePublish { get; init; }
    public int NumOfReceivers { get; init; } = 1;

    public required bool HasActivity { get; init; }

    public required bool HasDownstreamData { get; init; }

    public required bool HasBidirectionalData { get; init; }

    Task ISignalTransportConformityContextTestCase<FileSystemSignalTransportConformityTestHost>.BeforePublish(
        FileSystemSignalTransportConformityTestHost testHost
    ) => BeforePublish?.Invoke(testHost) ?? Task.CompletedTask;
}
