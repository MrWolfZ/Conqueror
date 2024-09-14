namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

// in a real application, instead use https://www.nuget.org/packages/Conqueror.CQS.Middleware.DataAnnotationValidation
internal sealed class DataAnnotationValidationCommandMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    public Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        // this will validate the object according to data annotation attributes and
        // will throw a ValidationException if validation fails
        Validator.ValidateObject(ctx.Command, new(ctx.Command), true);

        // if validation passes, execute the rest of the pipeline
        return ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
