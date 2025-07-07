namespace Conqueror.Transport.FileSystem.Tests.Signalling;

public abstract class FileSystemSignalConformityExecutionTestCase
    : FileSystemSignalConformityTestCase,
      ISignalTransportConformityExecutionTestCase<FileSystemSignalTransportConformityTestHost>
{
    public int NumOfReceivers { get; init; } = 1;

    public bool SignalsArePublishedInParallel { get; init; }

    public required IReadOnlyCollection<object> ExpectedReceivedSignals { get; init; }
}
