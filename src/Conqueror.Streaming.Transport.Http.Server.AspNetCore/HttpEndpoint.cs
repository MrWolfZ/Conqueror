using System;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal sealed record HttpEndpoint
{
    public required string Path { get; init; }

    public required string? Version { get; init; }

    public required string Name { get; init; }

    public required string OperationId { get; init; }

    public required string ControllerName { get; init; }

    public required string? ApiGroupName { get; init; }

    public required Type RequestType { get; init; }

    public required Type ItemType { get; init; }
}
