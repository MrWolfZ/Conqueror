namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed record MessageResponseMetadata(string MessageId, string? EncodedContextData);
