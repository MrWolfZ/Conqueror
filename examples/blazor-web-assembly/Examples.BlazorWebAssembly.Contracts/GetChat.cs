namespace Examples.BlazorWebAssembly.Contracts;

[HttpMessage<ChatEntry[]>(Version = "v1")]
public sealed partial record GetChat;

public sealed record ChatEntry
{
    public required DateTimeOffset Timestamp { get; init; }

    public required string User { get; init; }

    public required string Content { get; init; }
}
