using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Conqueror;

/// <summary>
///     This exception is thrown in the context of a Conqueror execution when the authenticated principal
///     is not authorized for an operation based on its payload.
/// </summary>
[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "we have equivalent constructors with one added parameter")]
public sealed class ConquerorOperationPayloadAuthorizationFailedException : ConquerorAuthorizationFailedException
{
    public ConquerorOperationPayloadAuthorizationFailedException(string message, ConquerorAuthorizationResult result)
        : base(message, result)
    {
    }

    public ConquerorOperationPayloadAuthorizationFailedException(string message, Exception innerException, ConquerorAuthorizationResult result)
        : base(message, innerException, result)
    {
    }

    public ConquerorOperationPayloadAuthorizationFailedException(ConquerorAuthorizationResult result)
        : base(result)
    {
    }

    private ConquerorOperationPayloadAuthorizationFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
