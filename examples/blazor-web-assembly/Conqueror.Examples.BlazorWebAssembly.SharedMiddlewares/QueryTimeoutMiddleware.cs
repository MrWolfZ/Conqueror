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

public static class TimeoutQueryPipelineBuilderExtensions
{
    public static IQueryPipelineBuilder UseTimeout(this IQueryPipelineBuilder pipeline, TimeSpan timeoutAfter)
    {
        return pipeline.Use<QueryTimeoutMiddleware, QueryTimeoutMiddlewareConfiguration>(new(timeoutAfter));
    }

    public static IQueryPipelineBuilder ConfigureTimeout(this IQueryPipelineBuilder pipeline, TimeSpan timeoutAfter)
    {
        return pipeline.Configure<QueryTimeoutMiddleware, QueryTimeoutMiddlewareConfiguration>(new QueryTimeoutMiddlewareConfiguration(timeoutAfter));
    }
}
