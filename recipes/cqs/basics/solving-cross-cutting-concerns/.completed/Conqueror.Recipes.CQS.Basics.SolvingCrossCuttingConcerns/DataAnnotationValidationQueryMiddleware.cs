namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal sealed class DataAnnotationValidationQueryMiddleware : IQueryMiddleware
{
    public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
        where TQuery : class
    {
        // this will validate the object according to data annotation validations and
        // will throw a ValidationException if validation fails
        Validator.ValidateObject(ctx.Query, new(ctx.Query), true);

        // if validation passes, execute the rest of the pipeline
        return ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
