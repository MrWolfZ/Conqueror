using System.Text.Json;

namespace Conqueror.Examples.CQS.GettingStarted.MiddlewaresExample;

public sealed record QueryLoggingMiddlewareConfiguration
{
    public bool LogException { get; set; } = true;

    public bool LogQueryPayload { get; set; } = true;

    public bool LogResponsePayload { get; set; } = true;
}

public sealed class QueryLoggingMiddleware : IQueryMiddleware
{
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ILoggerFactory loggerFactory;

    public QueryLoggingMiddleware(ILoggerFactory loggerFactory, JsonSerializerOptions jsonSerializerOptions)
    {
        this.loggerFactory = loggerFactory;
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    public required QueryLoggingMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
        where TQuery : class
    {
        var logger = loggerFactory.CreateLogger($"QueryHandler[{typeof(TQuery).Name},{typeof(TResponse).Name}]");

        try
        {
            if (Configuration.LogQueryPayload)
            {
                logger.LogInformation("Handling query of type {QueryType} with payload {QueryPayload}", typeof(TQuery).Name, Serialize(ctx.Query));
            }
            else
            {
                logger.LogInformation("Handling query of type {QueryType}", typeof(TQuery).Name);
            }

            var response = await ctx.Next(ctx.Query, ctx.CancellationToken);

            if (Configuration.LogResponsePayload)
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
            if (Configuration.LogException)
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

public static class LoggingQueryPipelineExtensions
{
    public static IQueryPipeline<TQuery, TResponse> UseLogging<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                  Action<QueryLoggingMiddlewareConfiguration>? configure = null)
        where TQuery : class
    {
        var configuration = new QueryLoggingMiddlewareConfiguration();
        configure?.Invoke(configuration);

        var loggerFactory = pipeline.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var jsonSerializerOptions = pipeline.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        return pipeline.Use(new QueryLoggingMiddleware(loggerFactory, jsonSerializerOptions) { Configuration = configuration });
    }

    public static IQueryPipeline<TQuery, TResponse> ConfigureLogging<TQuery, TResponse>(this IQueryPipeline<TQuery, TResponse> pipeline,
                                                                                        Action<QueryLoggingMiddlewareConfiguration> configure)
        where TQuery : class
    {
        return pipeline.Configure<QueryLoggingMiddleware>(m => configure(m.Configuration));
    }
}
