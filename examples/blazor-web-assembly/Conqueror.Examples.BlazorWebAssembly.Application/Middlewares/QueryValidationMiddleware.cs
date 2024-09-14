using System.ComponentModel.DataAnnotations;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class QueryValidationMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        Validator.ValidateObject(ctx.Query, new(ctx.Query), true);
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class ValidationQueryPipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseValidation<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use(new QueryValidationMiddleware<TQuery, TResponse>());
    }
}
