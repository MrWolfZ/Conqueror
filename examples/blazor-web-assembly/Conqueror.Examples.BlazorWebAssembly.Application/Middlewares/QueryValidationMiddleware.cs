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
    public static IQueryPipelineBuilder UseValidation(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.Use<QueryValidationMiddleware>();
    }
}
