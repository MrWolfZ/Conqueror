using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.DataAnnotationValidation;

/// <summary>
///     A query middleware which adds data annotation validation functionality to a query pipeline.
/// </summary>
public sealed class DataAnnotationValidationQueryMiddleware : IQueryMiddleware
{
    /// <inheritdoc />
    public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
        where TQuery : class
    {
        DataAnnotationValidator.ValidateObject(ctx.Query);

        return ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
