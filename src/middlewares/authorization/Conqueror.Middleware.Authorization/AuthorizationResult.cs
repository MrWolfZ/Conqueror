#pragma warning disable IDE0130 // Namespaces don't match folder structure - part of the public API

namespace Conqueror;

/// <summary>
///     Represents the result of a Conqueror authorization check.
/// </summary>
public abstract class AuthorizationResult;

public sealed class AuthorizationSuccessResult : AuthorizationResult
{
    public static readonly AuthorizationSuccessResult Instance = new();
}

public sealed class AuthorizationFailureResult(IReadOnlyCollection<string> details, string reason) : AuthorizationResult
{
    /// <summary>
    ///     The details for an authorization failure.
    /// </summary>
    public IReadOnlyCollection<string> Details { get; } = details;

    public string Reason { get; } = reason;
}
