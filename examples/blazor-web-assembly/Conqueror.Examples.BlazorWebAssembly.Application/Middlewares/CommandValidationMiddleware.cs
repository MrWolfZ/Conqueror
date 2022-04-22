using System.ComponentModel.DataAnnotations;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class ValidateCommandAttribute : CommandMiddlewareConfigurationAttribute
{
}

public sealed class CommandValidationMiddleware : ICommandMiddleware<ValidateCommandAttribute>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, ValidateCommandAttribute> ctx)
        where TCommand : class
    {
        Validator.ValidateObject(ctx.Command, new(ctx.Command), true);
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
