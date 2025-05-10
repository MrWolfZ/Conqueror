namespace Conqueror.Signalling.Sse;

internal sealed record HttpSseSignalEnvelope(object Signal, string? ContextData);
