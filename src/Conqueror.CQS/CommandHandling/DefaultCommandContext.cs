using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Conqueror.CQS.CommandHandling;

/// <inheritdoc />
internal sealed class DefaultCommandContext : ICommandContext
{
    private readonly Lazy<IDictionary<object, object?>> itemsLazy = new(() => new ConcurrentDictionary<object, object?>());

    public DefaultCommandContext(object command, string commandId)
    {
        Command = command;
        CommandId = commandId;
    }

    /// <inheritdoc />
    public object Command { get; private set; }

    /// <inheritdoc />
    public object? Response { get; private set; }

    /// <inheritdoc />
    public string CommandId { get; }

    /// <inheritdoc />
    public IDictionary<object, object?> Items => itemsLazy.Value;

    public void SetCommand(object command)
    {
        Command = command;
    }

    public void SetResponse(object? response)
    {
        Response = response;
    }
}
