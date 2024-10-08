﻿namespace Conqueror;

public static class CommandConquerorContextExtensions
{
    private const string CommandIdKey = "conqueror-command-id";

    /// <summary>
    ///     Get the ID of the currently executing command (if any).
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to get the command ID from</param>
    /// <returns>the ID of the executing command if there is one, otherwise <c>null</c></returns>
    public static string? GetCommandId(this ConquerorContext conquerorContext)
    {
        return conquerorContext.DownstreamContextData.Get<string>(CommandIdKey);
    }

    /// <summary>
    ///     Set the ID of the currently executing command.
    /// </summary>
    /// <param name="conquerorContext">the conqueror context to set the command ID in</param>
    /// <param name="commandId">the command ID to set</param>
    public static void SetCommandId(this ConquerorContext conquerorContext, string commandId)
    {
        conquerorContext.DownstreamContextData.Set(CommandIdKey, commandId, ConquerorContextDataScope.AcrossTransports);
    }
}
