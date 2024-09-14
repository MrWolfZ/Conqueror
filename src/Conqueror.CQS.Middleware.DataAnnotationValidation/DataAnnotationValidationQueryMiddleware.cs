using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.DataAnnotationValidation;

/// <summary>
///     A query middleware which adds data annotation validation functionality to a query pipeline.
/// </summary>
public sealed class DataAnnotationValidationQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    /// <inheritdoc />
    public Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        DataAnnotationValidator.ValidateObject(ctx.Query);

        return ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
