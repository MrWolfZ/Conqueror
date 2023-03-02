namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Contracts;

[HttpCommand]
public sealed record SetMostRecentlyIncrementedCounterForUserCommand([Required] string UserId, [Required] string CounterName);

public interface ISetMostRecentlyIncrementedCounterForUserCommandHandler : ICommandHandler<SetMostRecentlyIncrementedCounterForUserCommand>
{
}
