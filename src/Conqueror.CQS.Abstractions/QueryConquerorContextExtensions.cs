namespace Conqueror;

public static class QueryConquerorContextExtensions
{
    private const string QueryIdKey = "conqueror-query-id";

    /// <summary>
    ///     Get the ID of the currently executing query (if any).
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to get the query ID from</param>
    /// <returns>the ID of the executing query if there is one, otherwise <c>null</c></returns>
    public static string? GetQueryId(this ConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<string>(QueryIdKey);
    }

    /// <summary>
    ///     Set the ID of the currently executing query.
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to set the query ID in</param>
    /// <param name="queryId">the query ID to set</param>
    public static void SetQueryId(this ConquerorContext conquerorContext, string queryId)
    {
        conquerorContext.DownstreamContextData.Set(QueryIdKey, queryId, ConquerorContextDataScope.AcrossTransports);
    }
}
