using Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Contracts;

namespace Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Server;

internal sealed class GetCounterValueQueryHandler(CountersRepository repository) : IGetCounterValueQueryHandler
{
    public async Task<GetCounterValueQueryResponse> Handle(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(query.CounterName);
        return new(counterValue.HasValue, counterValue);
    }
}
