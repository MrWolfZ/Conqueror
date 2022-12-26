using System.ComponentModel.DataAnnotations;

namespace Conqueror.Examples.BlazorWebAssembly.Contracts;

[HttpCommand(Version = 1, ApiGroupName = "SharedCounter")]
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
