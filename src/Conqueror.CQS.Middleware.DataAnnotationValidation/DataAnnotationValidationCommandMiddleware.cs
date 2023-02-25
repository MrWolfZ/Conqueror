using System.Threading.Tasks;

namespace Conqueror.CQS.Middleware.DataAnnotationValidation
{
    /// <summary>
    ///     A command middleware which adds data annotation validation functionality to a command pipeline.
    /// </summary>
    public sealed class DataAnnotationValidationCommandMiddleware : ICommandMiddleware
    {
        public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            DataAnnotationValidator.ValidateObject(ctx.Command);

            return ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }
}
