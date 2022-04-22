namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class QueryTimeoutAttribute : QueryMiddlewareConfigurationAttribute
{
    public int TimeoutAfterSeconds { get; init; }
    
    public TimeSpan TimeoutAfter => TimeSpan.FromSeconds(TimeoutAfterSeconds);
}

public sealed class QueryTimeoutMiddleware : IQueryMiddleware<QueryTimeoutAttribute>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, QueryTimeoutAttribute> ctx)
        where TQuery : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
