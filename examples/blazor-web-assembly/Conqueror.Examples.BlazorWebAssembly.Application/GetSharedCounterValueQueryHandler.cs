using Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

namespace Conqueror.Examples.BlazorWebAssembly.Application;

internal sealed class GetSharedCounterValueQueryHandler : IGetSharedCounterValueQueryHandler
{
    private readonly SharedCounter counter;

    public GetSharedCounterValueQueryHandler(SharedCounter counter)
    {
        this.counter = counter;
    }

    [LogQuery]
    [ValidateQuery]
    public async Task<GetSharedCounterValueQueryResponse> ExecuteQuery(GetSharedCounterValueQuery query, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new(counter.GetValue());
    }
}
