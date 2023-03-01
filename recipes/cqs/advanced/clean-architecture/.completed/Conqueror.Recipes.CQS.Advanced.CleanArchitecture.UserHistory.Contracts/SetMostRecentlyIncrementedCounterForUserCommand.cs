namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Contracts;

public sealed record SetMostRecentlyIncrementedCounterForUserCommand([Required] string UserId, [Required] string CounterName);

public interface ISetMostRecentlyIncrementedCounterForUserCommandHandler : ICommandHandler<SetMostRecentlyIncrementedCounterForUserCommand>
{
}
