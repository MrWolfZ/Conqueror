namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

// in a real application, instead use https://www.nuget.org/packages/Conqueror.CQS.Middleware.DataAnnotationValidation
internal sealed class DataAnnotationValidationQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    public Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        // this will validate the object according to data annotation attributes and
        // will throw a ValidationException if validation fails
        Validator.ValidateObject(ctx.Query, new(ctx.Query), true);

        // if validation passes, execute the rest of the pipeline
        return ctx.Next(ctx.Query, ctx.CancellationToken);
    }
}
