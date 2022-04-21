namespace Conqueror.Examples.BlazorWebAssembly.Contracts;

[HttpCommand]
public sealed record DecrementSharedCounterValueCommand
{
    [Required]
    [Range(1, 100)]
    public long DecrementBy { get; init; }
}

public sealed record DecrementSharedCounterValueCommandResponse(long ValueAfterDecrement);

public interface IDecrementSharedCounterValueCommandHandler : ICommandHandler<DecrementSharedCounterValueCommand, DecrementSharedCounterValueCommandResponse>
{
}
