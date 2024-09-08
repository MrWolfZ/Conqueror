using System.ComponentModel.DataAnnotations;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class QueryValidationMiddleware : IQueryMiddleware
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
        where TQuery : class
    {
        Validator.ValidateObject(ctx.Query, new(ctx.Query), true);
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}

public static class ValidationQueryPipelineBuilderExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseValidation<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline)
        where TQuery : class
    {
        return pipeline.Use<QueryValidationMiddleware>();
    }
}
