using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.FileSystem.Tests.Messaging;

public sealed class FileSystemMessageConformityContextTestCase
    : FileSystemMessageConformityTestCase,
      IMessageTransportConformityContextTestCase<FileSystemMessageTransportConformityTestHost>
{
    public required bool HasActivity { get; init; }

    public required bool HasDownstreamData { get; init; }

    public required bool HasBidirectionalData { get; init; }

    public required bool HasUpstreamData { get; init; }
}
