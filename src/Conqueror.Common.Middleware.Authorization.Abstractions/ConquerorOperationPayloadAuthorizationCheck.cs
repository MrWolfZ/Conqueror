using System.Security.Claims;
using System.Threading.Tasks;

namespace Conqueror;

/// <summary>
///     A data authorization check evaluates whether a principal is allowed to execute a given operation based on its payload.
/// </summary>
/// <param name="principal">The principal to check authorization for</param>
/// <param name="payload">The payload (e.g. command or query) for which to check authorization for</param>
/// <typeparam name="TOperation">The Conqueror operation type (e.g. command or query type)</typeparam>
public delegate Task<ConquerorAuthorizationResult> ConquerorOperationPayloadAuthorizationCheck<in TOperation>(ClaimsPrincipal principal, TOperation payload)
    where TOperation : class;
