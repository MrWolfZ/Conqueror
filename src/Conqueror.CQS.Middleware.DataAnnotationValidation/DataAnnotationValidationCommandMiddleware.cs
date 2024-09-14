using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.DataAnnotationValidation;

/// <summary>
///     A command middleware which adds data annotation validation functionality to a command pipeline.
/// </summary>
public sealed class DataAnnotationValidationCommandMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    public Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        DataAnnotationValidator.ValidateObject(ctx.Command);

        return ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
