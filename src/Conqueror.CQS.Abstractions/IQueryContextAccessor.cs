namespace Conqueror;

/// <summary>
///     Provides access to the current <see cref="IQueryContext" />, if one is available.
/// </summary>
/// <remarks>
///     This interface should be used with caution. It relies on <see cref="System.Threading.AsyncLocal{T}" /> which can have a negative performance impact on async calls.
///     It also creates a dependency on "ambient state" which can make testing more difficult.
/// </remarks>
public interface IQueryContextAccessor
{
    /// <summary>
    ///     Gets the current <see cref="IQueryContext" />. Returns <see langword="null" /> if there is no active <see cref="IQueryContext" />.
    /// </summary>
    IQueryContext? QueryContext { get; }

    /// <summary>
    ///     Allows setting the <see cref="IQueryContext.QueryId" /> before calling a query handler.
    ///     This method is typically called from a server-side transport implementation and does not need to be called by user-code.
    /// </summary>
    /// <param name="queryId">The ID to set for the query</param>
    void SetExternalQueryId(string queryId);
}
