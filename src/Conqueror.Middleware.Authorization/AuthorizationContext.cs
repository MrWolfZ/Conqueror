﻿using System;
using System.Collections.Generic;

namespace Conqueror.Middleware.Authorization;

public abstract class AuthorizationContext
{
    public abstract IServiceProvider ServiceProvider { get; }

    public abstract ConquerorContext ConquerorContext { get; }

    /// <summary>
    ///     Create an authorization result that represents a successful authorization check.
    /// </summary>
    public AuthorizationResult Success() => AuthorizationSuccessResult.Instance;

    /// <summary>
    ///     Create an authorization result that represents a failed authentication check.
    /// </summary>
    /// <param name="details">The details for the authorization failure</param>
    public AuthorizationResult Unauthenticated(string details)
        => new AuthorizationFailureResult([details], MessageFailedException.WellKnownReasons.Unauthenticated);

    /// <summary>
    ///     Create an authorization result that represents a failed authentication check.
    /// </summary>
    /// <param name="details">The details for the authorization failure</param>
    public AuthorizationResult Unauthenticated(IReadOnlyCollection<string> details)
        => new AuthorizationFailureResult(details, MessageFailedException.WellKnownReasons.Unauthenticated);

    /// <summary>
    ///     Create an authorization result that represents a failed authorization check.
    /// </summary>
    /// <param name="details">The details for the authorization failure</param>
    public AuthorizationResult Unauthorized(string details)
        => new AuthorizationFailureResult([details], MessageFailedException.WellKnownReasons.Unauthorized);

    /// <summary>
    ///     Create an authorization result that represents a failed authorization check.
    /// </summary>
    /// <param name="details">The details for the authorization failure</param>
    public AuthorizationResult Unauthorized(IReadOnlyCollection<string> details)
        => new AuthorizationFailureResult(details, MessageFailedException.WellKnownReasons.Unauthorized);
}
