namespace Conqueror.Recipes.Messaging.CallingHttp.Server;

public class DataAnnotationValidationMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        Validator.ValidateObject(ctx.Message, new ValidationContext(ctx.Message), validateAllProperties: true);

        return ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}
