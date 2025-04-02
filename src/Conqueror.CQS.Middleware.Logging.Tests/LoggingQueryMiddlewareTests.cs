using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests;

[TestFixture]
public sealed class LoggingQueryMiddlewareTests : TestBase
{
    private const string TestTransportTypeName = "test-transport";

    private Func<TestQuery, TestQueryResponse> handlerFn = qry => new(qry.Payload);
    private Action<IQueryPipeline<TestQuery, TestQueryResponse>> configurePipeline = b => b.UseLogging();
    private Action<IQueryPipeline<TestQueryWithoutPayload, TestQueryResponse>> configurePipelineWithoutPayload = b => b.UseLogging();

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsQueryWithPayloadPreExecution()
    {
        var testQuery = new TestQuery(10);

        _ = await Handler.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsQueryThatHasNoPayloadPreExecution()
    {
        var testQuery = new TestQueryWithoutPayload();

        _ = await HandlerWithoutPayload.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsTransportTypePreExecution()
    {
        var testQuery = new TestQueryWithTransport();

        _ = await Resolve<IQueryHandler<TestQueryWithTransport, TestQueryResponse>>().WithPipeline(p => p.UseLogging()).Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information, null, TestTransportTypeName, QueryTransportRole.Client);
        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", TestTransportTypeName, QueryTransportRole.Server);
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsResponseWithPayloadPostExecution()
    {
        var testQuery = new TestQuery(10);

        _ = await Handler.Handle(testQuery);

        AssertPostExecutionLogMessage(LogLevel.Information, "{\"ResponsePayload\":10}");
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsQueryWithTransportTypePostExecution()
    {
        var testQuery = new TestQueryWithTransport();

        _ = await Resolve<IQueryHandler<TestQueryWithTransport, TestQueryResponse>>().WithPipeline(p => p.UseLogging(o => o.OmitJsonSerializedResponsePayload = true))
                                                                                     .Handle(testQuery);

        AssertPostExecutionLogMessage(LogLevel.Information, null, TestTransportTypeName, QueryTransportRole.Client);
        AssertPostExecutionLogMessage(LogLevel.Information, "{\"ResponsePayload\":10}", TestTransportTypeName, QueryTransportRole.Server);
    }

    [Test]
    public void GivenDefaultLoggingMiddlewareConfiguration_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTrace()
    {
        var testQuery = new TestQuery(10);
        var exception = new InvalidOperationException("test exception message");

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.Handle(testQuery));

        AssertLogEntryContains(LogLevel.Error, "An exception occurred while executing query");
        AssertLogEntryContains(LogLevel.Error, exception.Message);
        AssertLogEntryContains(LogLevel.Error, exception.StackTrace![..exception.StackTrace!.IndexOf($"{nameof(LoggingQueryMiddleware<TestQuery, TestQueryResponse>)}`2.{nameof(LoggingQueryMiddleware<TestQuery, TestQueryResponse>.Execute)}(", StringComparison.Ordinal)]);
        AssertLogEntryContains(LogLevel.Error, $"{nameof(GivenDefaultLoggingMiddlewareConfiguration_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTrace)}()");
        AssertLogEntryContains(LogLevel.Error, "Query ID: ");
        AssertLogEntryContains(LogLevel.Error, "Trace ID: ");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevel_LogsQueryWithPayloadPreExecutionAtSpecifiedLevel()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Debug, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevel_LogsQueryThatHasNoPayloadPreExecutionAtSpecifiedLevel()
    {
        var testQuery = new TestQueryWithoutPayload();

        configurePipelineWithoutPayload = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await HandlerWithoutPayload.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Debug);
    }

    [Test]
    public async Task GivenConfiguredPostExecutionLogLevel_LogsResponseWithPayloadPostExecutionAtSpecifiedLevel()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.PostExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.Handle(testQuery);

        AssertPostExecutionLogMessage(LogLevel.Debug, "{\"ResponsePayload\":10}");
    }

    [Test]
    public void GivenConfiguredExceptionLogLevel_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTraceAtSpecifiedLevel()
    {
        var testQuery = new TestQuery(10);
        var exception = new InvalidOperationException("test exception message");

        handlerFn = _ => throw exception;

        configurePipeline = b => b.UseLogging(o => o.ExceptionLogLevel = LogLevel.Critical);

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.Handle(testQuery));

        AssertLogEntryContains(LogLevel.Critical, "An exception occurred while executing query");
        AssertLogEntryContains(LogLevel.Critical, exception.Message);
        AssertLogEntryContains(LogLevel.Critical, exception.StackTrace![..exception.StackTrace!.IndexOf($"{nameof(LoggingQueryMiddleware<TestQuery, TestQueryResponse>)}`2.{nameof(LoggingQueryMiddleware<TestQuery, TestQueryResponse>.Execute)}(", StringComparison.Ordinal)]);
        AssertLogEntryContains(LogLevel.Critical, $"{nameof(GivenConfiguredExceptionLogLevel_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTraceAtSpecifiedLevel)}()");
        AssertLogEntryContains(LogLevel.Critical, "Query ID: ");
        AssertLogEntryContains(LogLevel.Critical, "Trace ID: ");
    }

    [Test]
    public async Task GivenConfiguredToOmitQueryPayload_LogsQueryWithoutPayloadPreExecution()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedQueryPayload = true);

        _ = await Handler.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitQueryPayload_LogsQueryThatHasNoPayloadPreExecution()
    {
        var testQuery = new TestQueryWithoutPayload();

        configurePipelineWithoutPayload = b => b.UseLogging(o => o.OmitJsonSerializedQueryPayload = true);

        _ = await HandlerWithoutPayload.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitResponsePayload_LogsResponseWithoutPayloadPostExecution()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedResponsePayload = true);

        _ = await Handler.Handle(testQuery);

        AssertPostExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredLoggerNameFactory_LogsWithCorrectName()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = qry => $"Custom{qry.GetType().Name}");

        _ = await Handler.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", loggerName: $"Custom{testQuery.GetType().Name}");
    }

    [Test]
    public async Task GivenConfiguredLoggerNameFactory_WhenFactoryReturnsNull_LogsWithLoggerWithDefaultName()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = _ => null);

        _ = await Handler.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", loggerName: testQuery.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPreExecutionHook_HookIsCalledWithCorrectParameters()
    {
        var testQuery = new TestQuery(10);
        LoggingQueryPreExecutionContext? seenContext = null;

        var queryId = string.Empty;
        var traceId = "test-trace-id";

        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(traceId);

        using var scope = Host.Services.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        configurePipeline = b => b.UseLogging(o =>
        {
            queryId = b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetQueryId();

            o.PreExecutionLogLevel = LogLevel.Debug;

            o.PreExecutionHook = ctx =>
            {
                seenContext = ctx;
                return true;
            };
        });

        _ = await handler.Handle(testQuery);

        seenContext?.Logger.LogCritical("validation");

        Assert.That(seenContext, Is.Not.Null);
        Assert.That(seenContext?.LogLevel, Is.EqualTo(LogLevel.Debug));
        Assert.That(seenContext?.QueryId, Is.SameAs(queryId));
        Assert.That(seenContext?.TraceId, Is.SameAs(traceId));
        Assert.That(seenContext?.Query, Is.SameAs(testQuery));
        Assert.That(seenContext?.ServiceProvider, Is.SameAs(scope.ServiceProvider));

        AssertLogEntryContains(LogLevel.Critical, "validation", testQuery.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPreExecutionHook_WhenHookReturnsFalse_PreExecutionMessageIsNotLogged()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.PreExecutionHook = _ => false);

        _ = await Handler.Handle(testQuery);

        AssertNoLogEntryContains(LogLevel.Information, "Executing query");
    }

    [Test]
    public async Task GivenConfiguredPostExecutionHook_HookIsCalledWithCorrectParameters()
    {
        var testQuery = new TestQuery(10);
        LoggingQueryPostExecutionContext? seenContext = null;

        var queryId = string.Empty;
        var traceId = "test-trace-id";

        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(traceId);

        using var scope = Host.Services.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        configurePipeline = b => b.UseLogging(o =>
        {
            queryId = b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetQueryId();

            o.PostExecutionLogLevel = LogLevel.Debug;

            o.PostExecutionHook = ctx =>
            {
                seenContext = ctx;
                return true;
            };
        });

        var response = await handler.Handle(testQuery);

        seenContext?.Logger.LogCritical("validation");

        Assert.That(seenContext, Is.Not.Null);
        Assert.That(seenContext?.LogLevel, Is.EqualTo(LogLevel.Debug));
        Assert.That(seenContext?.QueryId, Is.SameAs(queryId));
        Assert.That(seenContext?.TraceId, Is.SameAs(traceId));
        Assert.That(seenContext?.Query, Is.SameAs(testQuery));
        Assert.That(seenContext?.Response, Is.SameAs(response));
        Assert.That(seenContext?.ElapsedTime.Ticks, Is.GreaterThan(0));
        Assert.That(seenContext?.ServiceProvider, Is.SameAs(scope.ServiceProvider));

        AssertLogEntryContains(LogLevel.Critical, "validation", testQuery.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPostExecutionHook_WhenHookReturnsFalse_PostExecutionMessageIsNotLogged()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.PostExecutionHook = _ => false);

        _ = await Handler.Handle(testQuery);

        AssertNoLogEntryContains(LogLevel.Information, "Executed query");
    }

    [Test]
    public void GivenConfiguredExceptionHook_HookIsCalledWithCorrectParameters()
    {
        var testQuery = new TestQuery(10);
        var exception = new InvalidOperationException("test exception message");
        LoggingQueryExceptionContext? seenContext = null;

        var queryId = string.Empty;
        var traceId = "test-trace-id";

        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(traceId);

        using var scope = Host.Services.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        configurePipeline = b => b.UseLogging(o =>
        {
            queryId = b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetQueryId();

            o.ExceptionLogLevel = LogLevel.Critical;

            o.ExceptionHook = ctx =>
            {
                seenContext = ctx;
                return true;
            };
        });

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(testQuery));

        seenContext?.Logger.LogTrace("validation");

        Assert.That(seenContext, Is.Not.Null);
        Assert.That(seenContext?.LogLevel, Is.EqualTo(LogLevel.Critical));
        Assert.That(seenContext?.QueryId, Is.SameAs(queryId));
        Assert.That(seenContext?.TraceId, Is.SameAs(traceId));
        Assert.That(seenContext?.Query, Is.SameAs(testQuery));
        Assert.That(seenContext?.Exception, Is.SameAs(exception));
        Assert.That(seenContext!.ExecutionStackTrace.ToString(), Contains.Substring(nameof(GivenConfiguredExceptionHook_HookIsCalledWithCorrectParameters)));
        Assert.That(seenContext?.ElapsedTime.Ticks, Is.GreaterThan(0));
        Assert.That(seenContext?.ServiceProvider, Is.SameAs(scope.ServiceProvider));

        AssertLogEntryContains(LogLevel.Trace, "validation", testQuery.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public void GivenConfiguredExceptionHook_WhenHookReturnsFalse_ExceptionMessageIsNotLogged()
    {
        var testQuery = new TestQuery(10);
        var exception = new InvalidOperationException("test exception message");

        configurePipeline = b => b.UseLogging(o => o.ExceptionHook = _ => false);

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.Handle(testQuery));

        AssertNoLogEntryContains(LogLevel.Error, "An exception occurred while executing query");
    }

    [Test]
    public async Task GivenConfiguredJsonSerializerSettings_QueryIsSerializedWithConfiguredSettingsPreExecution()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _ = await Handler.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"payload\":10}");
    }

    [Test]
    public async Task GivenConfiguredJsonSerializerSettings_ResponseIsSerializedWithConfiguredSettingsPostExecution()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _ = await Handler.Handle(testQuery);

        AssertPostExecutionLogMessage(LogLevel.Information, "{\"responsePayload\":10}");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevelWhichIsNotActive_QueryIsNotSerialized()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o =>
        {
            o.PreExecutionLogLevel = LogLevel.None;
            o.OmitJsonSerializedResponsePayload = true;
            o.JsonSerializerOptions = new() { PropertyNamingPolicy = new ThrowingJsonNamingPolicy() };
        });

        _ = await Handler.Handle(testQuery);

        AssertNoLogEntryContains(LogLevel.None, "Executing query");
    }

    [Test]
    public async Task GivenConfiguredPostExecutionLogLevelWhichIsNotActive_ResponseIsNotSerialized()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o =>
        {
            o.PostExecutionLogLevel = LogLevel.None;
            o.OmitJsonSerializedQueryPayload = true;
            o.JsonSerializerOptions = new() { PropertyNamingPolicy = new ThrowingJsonNamingPolicy() };
        });

        _ = await Handler.Handle(testQuery);

        AssertNoLogEntryContains(LogLevel.None, "Executed query");
    }

    [Test]
    public async Task GivenTraceId_LogsCorrectTraceId()
    {
        var testQuery = new TestQuery(10);

        var traceId = "test-trace-id";

        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(traceId);

        _ = await Handler.Handle(testQuery);

        AssertLogEntryContains(LogLevel.Information, $"Trace ID: {traceId}", nrOfTimes: 2);
    }

    [Test]
    public async Task GivenQueryId_LogsCorrectQueryId()
    {
        var testQuery = new TestQuery(10);

        var queryId = string.Empty;

        configurePipeline = b =>
        {
            queryId = b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetQueryId();

            _ = b.UseLogging();
        };

        _ = await Handler.Handle(testQuery);

        AssertLogEntryContains(LogLevel.Information, $"Query ID: {queryId}", nrOfTimes: 2);
    }

    [Test]
    public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeConfigured()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging().ConfigureLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.Handle(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Debug, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeRemoved()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging().WithoutLogging();

        _ = await Handler.Handle(testQuery);

        AssertNoLogEntryContains(LogLevel.Debug, "Executing query");
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "middleware logic is to use lowercase")]
    private void AssertPreExecutionLogMessage(LogLevel logLevel,
                                              string? expectedSerializedQuery = null,
                                              string? expectedTransportTypeName = null,
                                              QueryTransportRole? expectedTransportRole = null,
                                              string? loggerName = null)
    {
        var transportTypeNameFragment = expectedTransportTypeName is null ? string.Empty : $" {expectedTransportTypeName}";
        var transportRoleFragment = expectedTransportRole is null ? string.Empty : $" on {Enum.GetName(expectedTransportRole.Value)?.ToLowerInvariant()}";

        if (expectedSerializedQuery is null)
        {
            var regexWithoutPayload = new Regex($@"Executing{transportTypeNameFragment} query{transportRoleFragment} \(Query ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
            AssertLogEntryMatches(logLevel, regexWithoutPayload, loggerName);
            return;
        }

        var regexWithPayload = new Regex($"Executing{transportTypeNameFragment} query{transportRoleFragment} with payload " + Regex.Escape(expectedSerializedQuery) + @" \(Query ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
        AssertLogEntryMatches(logLevel, regexWithPayload, loggerName);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "middleware logic is to use lowercase")]
    private void AssertPostExecutionLogMessage(LogLevel logLevel,
                                               string? expectedSerializedResponse = null,
                                               string? expectedTransportTypeName = null,
                                               QueryTransportRole? expectedTransportRole = null,
                                               string? loggerName = null)
    {
        var transportTypeNameFragment = expectedTransportTypeName is null ? string.Empty : $" {expectedTransportTypeName}";
        var transportRoleFragment = expectedTransportRole is null ? string.Empty : $" on {Enum.GetName(expectedTransportRole.Value)?.ToLowerInvariant()}";

        if (expectedSerializedResponse is null)
        {
            var regexWithoutPayload = new Regex($@"Executed{transportTypeNameFragment} query{transportRoleFragment} in [0-9.]+ms \(Query ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
            AssertLogEntryMatches(logLevel, regexWithoutPayload, loggerName);
            return;
        }

        var regexWithPayload = new Regex($"Executed{transportTypeNameFragment} query{transportRoleFragment} and got response " + Regex.Escape(expectedSerializedResponse) + @" in [0-9.]+ms \(Query ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
        AssertLogEntryMatches(logLevel, regexWithPayload, loggerName);
    }

    private IQueryHandler<TestQuery, TestQueryResponse> Handler => Resolve<IQueryHandler<TestQuery, TestQueryResponse>>();

    private IQueryHandler<TestQueryWithoutPayload, TestQueryResponse> HandlerWithoutPayload => Resolve<IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(
                        async (query, _, _) =>
                        {
                            await Task.Yield();
                            return handlerFn(query);
                        },
                        pipeline => configurePipeline(pipeline))
                    .AddConquerorQueryHandlerDelegate<TestQueryWithoutPayload, TestQueryResponse>(
                        async (_, _, _) =>
                        {
                            await Task.Yield();
                            return new(0);
                        },
                        pipeline => configurePipelineWithoutPayload(pipeline))
                    .AddConquerorQueryClient<IQueryHandler<TestQueryWithTransport, TestQueryResponse>>(_ => new TestQueryTransportClient());
    }

    private sealed record TestQuery(int Payload);

    private sealed record TestQueryResponse(int ResponsePayload);

    private sealed record TestQueryWithoutPayload;

    private sealed record TestQueryWithTransport;

    private sealed class ThrowingJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestQueryTransportClient : IQueryTransportClient
    {
        public string TransportTypeName => TestTransportTypeName;

        public async Task<TResponse> Send<TQuery, TResponse>(TQuery query, IServiceProvider serviceProvider, CancellationToken cancellationToken)
            where TQuery : class
        {
            serviceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.SignalExecutionFromTransport(TestTransportTypeName);
            var handler = serviceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var response = await handler.Handle(new(10), cancellationToken);
            return (TResponse)(object)new TestQueryResponse(response.ResponsePayload + 10);
        }
    }
}
