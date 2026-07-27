namespace Conqueror.Recipes.Messaging.TestingCallingHttp.Middlewares;

// in a real application, instead use a shared validation middleware from a common library
public class DataAnnotationValidationMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        // this will validate the object according to data annotation attributes and
        // will throw a ValidationException if validation fails
        Validator.ValidateObject(ctx.Message, new ValidationContext(ctx.Message), validateAllProperties: true);

        // if validation passes, execute the rest of the pipeline
        return ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}
