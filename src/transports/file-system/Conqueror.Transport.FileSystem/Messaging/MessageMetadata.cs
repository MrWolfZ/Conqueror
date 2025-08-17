namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed record MessageMetadata(
    string MessageId,
    string? EncodedContextData,
    DateTimeOffset SentAtUtc,
    TimeSpan? TimeToLive,
    int NrOfFailedProcessingAttempts
);
