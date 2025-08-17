#pragma warning disable IDE0130 // Namespaces don't match folder structure - part of the public API

namespace Conqueror;

using System.Diagnostics.CodeAnalysis;

/// <summary>
///     An exception that represents badly formatted Conqueror context data.
/// </summary>
/// <param name="result">The failed authorization result</param>
[SuppressMessage(
    "Roslynator",
    "RCS1194:Implement exception constructors",
    Justification = "the standard constructors don't make sens here"
)]
public sealed class MessageAuthorizationFailedException(AuthorizationFailureResult result)
    : MessageFailedException(string.Join(Environment.NewLine, result.Details))
{
    public AuthorizationFailureResult Result { get; } = result;

    public override string WellKnownReason => Result.Reason;
}
