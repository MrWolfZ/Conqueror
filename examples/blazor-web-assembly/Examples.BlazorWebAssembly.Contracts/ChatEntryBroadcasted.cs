namespace Examples.BlazorWebAssembly.Contracts;

[HttpSseSignal]
public sealed partial record ChatEntryBroadcasted
{
    public required string User { get; init; }

    public required string Content { get; init; }

    public required DateTimeOffset Timestamp { get; init; }
}
