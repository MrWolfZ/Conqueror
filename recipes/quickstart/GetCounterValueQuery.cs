using Conqueror;

namespace Quickstart;

[HttpQuery(Version = "v1")]
public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>;

internal sealed class GetCounterValueQueryHandler : IGetCounterValueQueryHandler
{
    public static void ConfigurePipeline(IQueryPipeline<GetCounterValueQuery, GetCounterValueQueryResponse> pipeline) =>
        pipeline.UseLogging(o => o.ExceptionLogLevel = LogLevel.Critical);

    public async Task<GetCounterValueQueryResponse> Handle(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        // simulate an asynchronous operation
        await Task.CompletedTask;

        var envVariableName = $"QUICKSTART_COUNTERS_{query.CounterName}";
        var counterValue = int.Parse(Environment.GetEnvironmentVariable(envVariableName) ?? "0");
        return new(counterValue);
    }
}
