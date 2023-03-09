// ReSharper disable once CheckNamespace (we want these extensions to be accessible without an extra import)
namespace Conqueror;

public static class ConquerorContextCommandExtensions
{
    private const string CommandIdKey = "conqueror-command-id";

    /// <summary>
    ///     Get the ID of the currently executing command (if any).
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to get the command ID from</param>
    /// <returns>The ID of the executing command if there is one, otherwise <c>null</c></returns>
    public static string? GetCommandId(this IConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<string>(CommandIdKey);
    }

    internal static void SetCommandId(this IConquerorContext conquerorContext, string commandId)
    {
        conquerorContext.DownstreamContextData.Set(CommandIdKey, commandId, ConquerorContextDataScope.AcrossTransports);
    }
}
