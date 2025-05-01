namespace Examples.BlazorWebAssembly.Contracts;

[HttpMessage(Version = "v1")]
public sealed partial record BroadcastChatEntry
{
    public required string User { get; init; }

    public required string Content { get; init; }
}
