namespace Conqueror.Transport.FileSystem.Tests.Signalling;

public sealed class FileSystemSignalConformityExecutionSuccessTestCase
    : FileSystemSignalConformityExecutionTestCase,
      ISignalTransportConformityExecutionSuccessTestCase<FileSystemSignalTransportConformityTestHost>
{
    public Func<FileSystemSignalTransportConformityTestHost, Task>? BeforePublish { get; init; }

    public Func<FileSystemSignalTransportConformityTestHost, Task>? AfterSignalsAreReceived { get; init; }
    public bool ShouldCompleteImmediately { get; init; }

    Task ISignalTransportConformityExecutionSuccessTestCase<FileSystemSignalTransportConformityTestHost>.BeforePublish(
        FileSystemSignalTransportConformityTestHost testHost
    ) => BeforePublish?.Invoke(testHost) ?? Task.CompletedTask;

    Task ISignalTransportConformityExecutionSuccessTestCase<FileSystemSignalTransportConformityTestHost>.
        AfterSignalsAreReceived(
            FileSystemSignalTransportConformityTestHost testHost
        ) => AfterSignalsAreReceived?.Invoke(testHost) ?? Task.CompletedTask;
}
