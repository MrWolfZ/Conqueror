using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Conqueror;

/// <summary>
/// A functional authorization check evaluates whether a principal is allowed to execute a given operation type.
/// </summary>
/// <param name="principal">The principal to check authorization for</param>
/// <param name="conquerorOperationType">The Conqueror operation type (e.g. command or query type) for which to check authorization for</param>
public delegate Task<ConquerorAuthorizationResult> ConquerorFunctionalAuthorizationCheck(ClaimsPrincipal principal, Type conquerorOperationType);
