using Conqueror;

namespace Quickstart;

[HttpQuery(Version = "v1")]
public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>
{
}

internal sealed class GetCounterValueQueryHandler : IGetCounterValueQueryHandler
{
    // add logging to the query pipeline and configure the pre-execution log level (only for demonstration purposes since the default is the same)
    public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Information);

    public async Task<GetCounterValueQueryResponse> ExecuteQuery(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        // simulate an asynchronous operation
        await Task.CompletedTask;

        var envVariableName = $"QUICKSTART_COUNTERS_{query.CounterName}";
        var counterValue = int.Parse(Environment.GetEnvironmentVariable(envVariableName) ?? "0");
        return new(counterValue);
    }
}
