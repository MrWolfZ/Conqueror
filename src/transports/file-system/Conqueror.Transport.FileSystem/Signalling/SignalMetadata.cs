namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed record SignalMetadata(
    string SignalId,
    string? EncodedContextData,
    DateTimeOffset PublishedAtUtc);
