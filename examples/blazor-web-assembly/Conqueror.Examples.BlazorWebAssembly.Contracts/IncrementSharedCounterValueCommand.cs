using System.ComponentModel.DataAnnotations;

namespace Conqueror.Examples.BlazorWebAssembly.Contracts;

[HttpCommand]
public sealed record IncrementSharedCounterValueCommand
{
    [Required]
    [Range(1, 100)]
    public long IncrementBy { get; init; }
}

public sealed record IncrementSharedCounterValueCommandResponse(long ValueAfterIncrement);

public interface IIncrementSharedCounterValueCommandHandler : ICommandHandler<IncrementSharedCounterValueCommand, IncrementSharedCounterValueCommandResponse>
{
}
