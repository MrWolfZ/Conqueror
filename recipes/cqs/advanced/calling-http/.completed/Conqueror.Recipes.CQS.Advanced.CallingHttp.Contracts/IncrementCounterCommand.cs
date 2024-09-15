using System.ComponentModel.DataAnnotations;

namespace Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts;

[HttpCommand(Version = "v1")]
public sealed record IncrementCounterCommand([Required] string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

public interface IIncrementCounterCommandHandler : ICommandHandler<IncrementCounterCommand, IncrementCounterCommandResponse>;
