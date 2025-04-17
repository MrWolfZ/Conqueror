using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     An exception that represents badly formatted Conqueror context data.
/// </summary>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "they make no sense here")]
public sealed class MessageAuthorizationFailedException(AuthorizationFailureResult result)
    : MessageFailedException(string.Join(Environment.NewLine, result.Details))
{
    public AuthorizationFailureResult Result { get; } = result;

    public override string WellKnownReason => Result.Reason;
}
