using System.ComponentModel.DataAnnotations;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class ValidateQueryAttribute : QueryMiddlewareConfigurationAttribute
{
}

public sealed class QueryValidationMiddleware : IQueryMiddleware<ValidateQueryAttribute>
{
    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, ValidateQueryAttribute> ctx)
        where TQuery : class
    {
        Validator.ValidateObject(ctx.Query, new(ctx.Query), true);
        return await ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
