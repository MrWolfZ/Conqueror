using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Conqueror;

/// <summary>
///     Represents the result of a Conqueror authorization check.
/// </summary>
[Serializable]
public sealed class ConquerorAuthorizationResult : ISerializable
{
    private ConquerorAuthorizationResult()
    {
        IsSuccess = true;
    }

    private ConquerorAuthorizationResult(IReadOnlyCollection<string> failureReasons)
    {
        IsSuccess = false;
        FailureReasons = failureReasons;
    }

    private ConquerorAuthorizationResult(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {
        ArgumentNullException.ThrowIfNull(serializationInfo);

        IsSuccess = serializationInfo.GetBoolean("IsSuccess"); // Do not rename (binary serialization)
        FailureReasons = (string[])serializationInfo.GetValue("FailureReasons", typeof(string[]))!; // Do not rename (binary serialization)
    }

    /// <summary>
    ///     Whether the authorization was successful or not.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     The reasons for an authorization failure. May be empty for authorization failures.
    ///     Will always be empty if the authorization was successful.
    /// </summary>
    public IReadOnlyCollection<string> FailureReasons { get; } = ArraySegment<string>.Empty;

    /// <summary>
    ///     Create an authorization result that represents a successful authorization check.
    /// </summary>
    public static ConquerorAuthorizationResult Success() => new();

    /// <summary>
    ///     Create an authorization result that represents a failed authorization check.
    /// </summary>
    /// <param name="reason">The reason for the authorization failure</param>
    public static ConquerorAuthorizationResult Failure(string reason) => new(new[] { reason });

    /// <summary>
    ///     Create an authorization result that represents a failed authorization check.
    /// </summary>
    /// <param name="reasons">The reasons for the authorization failure</param>
    public static ConquerorAuthorizationResult Failure(IReadOnlyCollection<string> reasons) => new(reasons);

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("IsSuccess", IsSuccess); // Do not rename (binary serialization)
        info.AddValue("FailureReasons", FailureReasons.ToArray()); // Do not rename (binary serialization)
    }
}
