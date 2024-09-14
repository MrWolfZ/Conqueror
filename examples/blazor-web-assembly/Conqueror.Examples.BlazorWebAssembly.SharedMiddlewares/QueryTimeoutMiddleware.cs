namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record QueryTimeoutMiddlewareConfiguration
{
    public required TimeSpan TimeoutAfter { get; set; }
}

public sealed class QueryTimeoutMiddleware : IQueryMiddleware
{
    public required QueryTimeoutMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class TimeoutQueryPipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseTimeout<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                  TimeSpan timeoutAfter)
        where TQuery : class
    {
        return pipeline.Use(new QueryTimeoutMiddleware { Configuration = new() { TimeoutAfter = timeoutAfter } });
    }

    public static IQueryPipeline<TQuery, TResponse> ConfigureTimeout<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                        TimeSpan timeoutAfter)
        where TQuery : class
    {
        return pipeline.Configure<QueryTimeoutMiddleware>(m => m.Configuration.TimeoutAfter = timeoutAfter);
    }
}
