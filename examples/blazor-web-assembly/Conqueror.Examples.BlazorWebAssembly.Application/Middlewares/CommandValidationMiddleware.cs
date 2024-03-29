using System.ComponentModel.DataAnnotations;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class CommandValidationMiddleware : ICommandMiddleware
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        Validator.ValidateObject(ctx.Command, new(ctx.Command), true);
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class ValidationCommandPipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseValidation(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Use<CommandValidationMiddleware>();
    }
}
