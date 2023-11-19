namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;

internal sealed record HttpEndpoint
{
    public string Path { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string OperationId { get; init; } = default!;

    public string ControllerName { get; init; } = default!;

    public string? ApiGroupName { get; init; }
}
