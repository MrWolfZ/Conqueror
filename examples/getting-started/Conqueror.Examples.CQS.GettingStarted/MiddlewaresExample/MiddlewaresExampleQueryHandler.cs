namespace Conqueror.Examples.CQS.GettingStarted.MiddlewaresExample;

public sealed record MiddlewaresExampleQuery(int Parameter);

public sealed record MiddlewaresExampleQueryResponse(int Value);

public interface IMiddlewaresExampleQueryHandler : IQueryHandler<MiddlewaresExampleQuery, MiddlewaresExampleQueryResponse>
{
}

public sealed class MiddlewaresExampleQueryHandler : IMiddlewaresExampleQueryHandler, IConfigureQueryPipeline
{
    // ReSharper disable once UnusedMember.Global
    public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) =>
        pipeline.UseLogging(o => o with { LogQueryPayload = false });

    public async Task<MiddlewaresExampleQueryResponse> ExecuteQuery(MiddlewaresExampleQuery command, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(command.Parameter);
    }
}
