namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record QueryTimeoutMiddlewareConfiguration(TimeSpan TimeoutAfter);

public sealed class QueryTimeoutMiddleware : IQueryMiddleware<QueryTimeoutMiddlewareConfiguration>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, QueryTimeoutMiddlewareConfiguration> ctx)
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
        return pipeline.Use<QueryTimeoutMiddleware, QueryTimeoutMiddlewareConfiguration>(new(timeoutAfter));
    }

    public static IQueryPipeline<TQuery, TResponse> ConfigureTimeout<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                        TimeSpan timeoutAfter)
        where TQuery : class
    {
        return pipeline.Configure<QueryTimeoutMiddleware, QueryTimeoutMiddlewareConfiguration>(new QueryTimeoutMiddlewareConfiguration(timeoutAfter));
    }
}
