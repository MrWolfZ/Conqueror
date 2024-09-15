namespace Conqueror.Examples.CQS.GettingStarted.MiddlewaresExample;

public sealed record MiddlewaresExampleQuery(int Parameter);

public sealed record MiddlewaresExampleQueryResponse(int Value);

public interface IMiddlewaresExampleQueryHandler : IQueryHandler<MiddlewaresExampleQuery, MiddlewaresExampleQueryResponse>;

public sealed class MiddlewaresExampleQueryHandler : IMiddlewaresExampleQueryHandler
{
    // ReSharper disable once UnusedMember.Global
    public static void ConfigurePipeline(IQueryPipeline<MiddlewaresExampleQuery, MiddlewaresExampleQueryResponse> pipeline) =>
        pipeline.UseLogging(o => o.LogQueryPayload = false);

    public async Task<MiddlewaresExampleQueryResponse> Handle(MiddlewaresExampleQuery command, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return new(command.Parameter);
    }
}
