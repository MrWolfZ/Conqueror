namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class RetryCommandAttribute : CommandMiddlewareConfigurationAttribute
{
    public int MaxNumberOfAttempts { get; init; } = 3;

    public int RetryIntervalInSeconds { get; init; } = 1;
    
    public TimeSpan RetryInterval => TimeSpan.FromSeconds(RetryIntervalInSeconds);
}

public sealed class CommandRetryMiddleware : ICommandMiddleware<RetryCommandAttribute>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, RetryCommandAttribute> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
