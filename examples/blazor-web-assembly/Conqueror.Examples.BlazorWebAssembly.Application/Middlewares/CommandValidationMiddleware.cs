using System.ComponentModel.DataAnnotations;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class CommandValidationMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        Validator.ValidateObject(ctx.Command, new(ctx.Command), true);
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}

public static class ValidationCommandPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseValidation<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline)
        where TCommand : class
    {
        return pipeline.Use(new CommandValidationMiddleware<TCommand, TResponse>());
    }
}
