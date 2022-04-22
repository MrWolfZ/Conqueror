namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class CommandTimeoutAttribute : CommandMiddlewareConfigurationAttribute
{
    public int TimeoutAfterSeconds { get; init; }
    
    public TimeSpan TimeoutAfter => TimeSpan.FromSeconds(TimeoutAfterSeconds);
}

public sealed class CommandTimeoutMiddleware : ICommandMiddleware<CommandTimeoutAttribute>
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, CommandTimeoutAttribute> ctx)
        where TCommand : class
    {
        // .. in a real application you would place the logic here
        return await ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
