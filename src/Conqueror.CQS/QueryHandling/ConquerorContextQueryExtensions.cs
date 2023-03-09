// ReSharper disable once CheckNamespace (we want these extensions to be accessible without an extra import)
namespace Conqueror;

public static class ConquerorContextQueryExtensions
{
    private const string QueryIdKey = "conqueror-query-id";

    /// <summary>
    ///     Get the ID of the currently executing query (if any).
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to get the query ID from</param>
    /// <returns>The ID of the executing query if there is one, otherwise <c>null</c></returns>
    public static string? GetQueryId(this IConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.TryGet<string>(QueryIdKey, out var queryId) ? queryId : null;
    }

    internal static void SetQueryId(this IConquerorContext conquerorContext, string queryId)
    {
        conquerorContext.DownstreamContextData.Set(QueryIdKey, queryId, ConquerorContextDataScope.AcrossTransports);
    }
}
