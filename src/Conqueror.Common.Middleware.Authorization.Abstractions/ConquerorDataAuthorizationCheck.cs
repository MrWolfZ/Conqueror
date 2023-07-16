using System.Security.Claims;
using System.Threading.Tasks;

namespace Conqueror;

/// <summary>
/// A data authorization check evaluates whether a principal is allowed to execute a given operation.
/// </summary>
/// <param name="principal">The principal to check authorization for</param>
/// <param name="operation">The Conqueror operation (e.g. command or query) for which to check authorization for</param>
/// <typeparam name="TOperation">The Conqueror operation type (e.g. command or query type)</typeparam>
public delegate Task<ConquerorAuthorizationResult> ConquerorDataAuthorizationCheck<in TOperation>(ClaimsPrincipal principal, TOperation operation)
    where TOperation : class;
