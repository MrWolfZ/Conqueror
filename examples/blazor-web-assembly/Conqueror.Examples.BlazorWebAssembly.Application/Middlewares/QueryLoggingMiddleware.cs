namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class LogQueryAttribute : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<QueryLoggingMiddleware>
{
    public bool LogException { get; init; } = true;

    public bool LogQueryPayload { get; init; } = true;

    public bool LogResponsePayload { get; init; } = true;
}

public sealed class QueryLoggingMiddleware : IQueryMiddleware<LogQueryAttribute>
{
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ILoggerFactory loggerFactory;

    public QueryLoggingMiddleware(ILoggerFactory loggerFactory, JsonSerializerOptions jsonSerializerOptions)
    {
        this.loggerFactory = loggerFactory;
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, LogQueryAttribute> ctx)
        where TQuery : class
    {
        var logger = loggerFactory.CreateLogger($"QueryHandler[{typeof(TQuery).Name},{typeof(TResponse).Name}]");

        try
        {
            if (ctx.Configuration.LogQueryPayload)
            {
                logger.LogInformation("Handling query of type {QueryType} with payload {QueryPayload}", typeof(TQuery).Name, Serialize(ctx.Query));
            }
            else
            {
                logger.LogInformation("Handling query of type {QueryType}", typeof(TQuery).Name);
            }

            var response = await ctx.Next(ctx.Query, ctx.CancellationToken);

            if (ctx.Configuration.LogResponsePayload)
            {
                logger.LogInformation("Handled query of type {QueryType} and got response {ResponsePayload}", typeof(TQuery).Name, Serialize(response));
            }
            else
            {
                logger.LogInformation("Handled query of type {QueryType}", typeof(TQuery).Name);
            }

            return response;
        }
        catch (Exception e)
        {
            if (ctx.Configuration.LogException)
            {
                logger.LogError(e, "An exception occurred while handling query of type {QueryType}!", typeof(TQuery).Name);
            }
            else
            {
                logger.LogError("An exception occurred while handling query of type {QueryType}!", typeof(TQuery).Name);
            }

            throw;
        }
    }

    private string Serialize<T>(T value) => JsonSerializer.Serialize(value, jsonSerializerOptions);
}
