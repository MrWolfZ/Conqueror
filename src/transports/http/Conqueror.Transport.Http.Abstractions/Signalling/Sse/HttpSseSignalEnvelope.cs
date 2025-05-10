using System;

namespace Conqueror.Signalling.Sse;

internal sealed record HttpSseSignalEnvelope(object Signal, Type SignalType, string? ContextData);
