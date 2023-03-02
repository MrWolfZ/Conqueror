using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests;

[TestFixture]
public sealed class LoggingQueryMiddlewareTests : TestBase
{
    private Func<TestQuery, TestQueryResponse> handlerFn = qry => new(qry.Payload);
    private Action<IQueryPipelineBuilder> configurePipeline = b => b.UseLogging();

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsQueryWithPayloadPreExecution()
    {
        var testQuery = new TestQuery(10);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsQueryThatHasNoPayloadPreExecution()
    {
        var testQuery = new TestQueryWithoutPayload();

        _ = await HandlerWithoutPayload.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsResponseWithPayloadPostExecution()
    {
        var testQuery = new TestQuery(10);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPostExecutionLogMessage(LogLevel.Information, "{\"ResponsePayload\":10}");
    }

    [Test]
    public void GivenDefaultLoggingMiddlewareConfiguration_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTrace()
    {
        var testQuery = new TestQuery(10);
        var exception = new InvalidOperationException("test exception message");

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

        AssertLogEntryContains(LogLevel.Error, "An exception occurred while executing query");
        AssertLogEntryContains(LogLevel.Error, exception.Message);
        AssertLogEntryContains(LogLevel.Error, exception.StackTrace![..exception.StackTrace!.IndexOf("---", StringComparison.Ordinal)]);
        AssertLogEntryContains(LogLevel.Error, "Query ID: ");
        AssertLogEntryContains(LogLevel.Error, "Trace ID: ");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevel_LogsQueryWithPayloadPreExecutionAtSpecifiedLevel()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Debug, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevel_LogsQueryThatHasNoPayloadPreExecutionAtSpecifiedLevel()
    {
        var testQuery = new TestQueryWithoutPayload();

        configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await HandlerWithoutPayload.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Debug);
    }

    [Test]
    public async Task GivenConfiguredPostExecutionLogLevel_LogsResponseWithPayloadPostExecutionAtSpecifiedLevel()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.PostExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPostExecutionLogMessage(LogLevel.Debug, "{\"ResponsePayload\":10}");
    }

    [Test]
    public void GivenConfiguredExceptionLogLevel_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTraceAtSpecifiedLevel()
    {
        var testQuery = new TestQuery(10);
        var exception = new InvalidOperationException("test exception message");

        handlerFn = _ => throw exception;

        configurePipeline = b => b.UseLogging(o => o.ExceptionLogLevel = LogLevel.Critical);

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

        AssertLogEntryContains(LogLevel.Critical, "An exception occurred while executing query");
        AssertLogEntryContains(LogLevel.Critical, exception.Message);
        AssertLogEntryContains(LogLevel.Critical, exception.StackTrace![..exception.StackTrace!.IndexOf("---", StringComparison.Ordinal)]);
        AssertLogEntryContains(LogLevel.Critical, "Query ID: ");
        AssertLogEntryContains(LogLevel.Critical, "Trace ID: ");
    }

    [Test]
    public async Task GivenConfiguredToOmitQueryPayload_LogsQueryWithoutPayloadPreExecution()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedQueryPayload = true);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitQueryPayload_LogsQueryThatHasNoPayloadPreExecution()
    {
        var testQuery = new TestQueryWithoutPayload();

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedQueryPayload = true);

        _ = await HandlerWithoutPayload.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitResponsePayload_LogsResponseWithoutPayloadPostExecution()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedResponsePayload = true);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPostExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredLoggerNameFactory_LogsWithCorrectName()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = qry => $"Custom{qry.GetType().Name}");

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", $"Custom{testQuery.GetType().Name}");
    }

    [Test]
    public async Task GivenConfiguredLoggerNameFactory_WhenFactoryReturnsNull_LogsWithLoggerWithDefaultName()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = _ => null);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", testQuery.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPreExecutionHook_HookIsCalledWithCorrectParameters()
    {
        var testQuery = new TestQuery(10);
        LoggingQueryPreExecutionContext? seenContext = null;

        var queryId = "test-query-id";
        var traceId = "test-trace-id";

        Resolve<IQueryContextAccessor>().SetExternalQueryId(queryId);
        Resolve<IConquerorContextAccessor>().GetOrCreate().SetTraceId(traceId);

        using var scope = Host.Services.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        configurePipeline = b => b.UseLogging(o =>
        {
            o.PreExecutionLogLevel = LogLevel.Debug;

            o.PreExecutionHook = ctx =>
            {
                seenContext = ctx;
                return true;
            };
        });

        _ = await handler.ExecuteQuery(testQuery);

        seenContext?.Logger.LogCritical("validation");

        Assert.That(seenContext, Is.Not.Null);
        Assert.AreEqual(LogLevel.Debug, seenContext?.LogLevel);
        Assert.AreSame(queryId, seenContext?.QueryId);
        Assert.AreSame(traceId, seenContext?.TraceId);
        Assert.AreSame(testQuery, seenContext?.Query);
        Assert.AreSame(scope.ServiceProvider, seenContext?.ServiceProvider);

        AssertLogEntryContains(LogLevel.Critical, "validation", testQuery.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPreExecutionHook_WhenHookReturnsFalse_PreExecutionMessageIsNotLogged()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.PreExecutionHook = _ => false);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertNoLogEntryContains(LogLevel.Information, "Executing query");
    }

    [Test]
    public async Task GivenConfiguredPostExecutionHook_HookIsCalledWithCorrectParameters()
    {
        var testQuery = new TestQuery(10);
        LoggingQueryPostExecutionContext? seenContext = null;

        var queryId = "test-query-id";
        var traceId = "test-trace-id";

        Resolve<IQueryContextAccessor>().SetExternalQueryId(queryId);
        Resolve<IConquerorContextAccessor>().GetOrCreate().SetTraceId(traceId);

        using var scope = Host.Services.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        configurePipeline = b => b.UseLogging(o =>
        {
            o.PostExecutionLogLevel = LogLevel.Debug;

            o.PostExecutionHook = ctx =>
            {
                seenContext = ctx;
                return true;
            };
        });

        var response = await handler.ExecuteQuery(testQuery);

        seenContext?.Logger.LogCritical("validation");

        Assert.That(seenContext, Is.Not.Null);
        Assert.AreEqual(LogLevel.Debug, seenContext?.LogLevel);
        Assert.AreSame(queryId, seenContext?.QueryId);
        Assert.AreSame(traceId, seenContext?.TraceId);
        Assert.AreSame(testQuery, seenContext?.Query);
        Assert.AreSame(response, seenContext?.Response);
        Assert.IsTrue(seenContext?.ElapsedTime.Ticks > 0);
        Assert.AreSame(scope.ServiceProvider, seenContext?.ServiceProvider);

        AssertLogEntryContains(LogLevel.Critical, "validation", testQuery.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPostExecutionHook_WhenHookReturnsFalse_PostExecutionMessageIsNotLogged()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.PostExecutionHook = _ => false);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertNoLogEntryContains(LogLevel.Information, "Executed query");
    }

    [Test]
    public void GivenConfiguredExceptionHook_HookIsCalledWithCorrectParameters()
    {
        var testQuery = new TestQuery(10);
        var exception = new InvalidOperationException("test exception message");
        LoggingQueryExceptionContext? seenContext = null;

        var queryId = "test-query-id";
        var traceId = "test-trace-id";

        Resolve<IQueryContextAccessor>().SetExternalQueryId(queryId);
        Resolve<IConquerorContextAccessor>().GetOrCreate().SetTraceId(traceId);

        using var scope = Host.Services.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        configurePipeline = b => b.UseLogging(o =>
        {
            o.ExceptionLogLevel = LogLevel.Critical;

            o.ExceptionHook = ctx =>
            {
                seenContext = ctx;
                return true;
            };
        });

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(testQuery));

        seenContext?.Logger.LogTrace("validation");

        Assert.That(seenContext, Is.Not.Null);
        Assert.AreEqual(LogLevel.Critical, seenContext?.LogLevel);
        Assert.AreSame(queryId, seenContext?.QueryId);
        Assert.AreSame(traceId, seenContext?.TraceId);
        Assert.AreSame(testQuery, seenContext?.Query);
        Assert.AreSame(exception, seenContext?.Exception);
        Assert.IsTrue(seenContext?.ElapsedTime.Ticks > 0);
        Assert.AreSame(scope.ServiceProvider, seenContext?.ServiceProvider);

        AssertLogEntryContains(LogLevel.Trace, "validation", testQuery.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public void GivenConfiguredExceptionHook_WhenHookReturnsFalse_ExceptionMessageIsNotLogged()
    {
        var testQuery = new TestQuery(10);
        var exception = new InvalidOperationException("test exception message");

        configurePipeline = b => b.UseLogging(o => o.ExceptionHook = _ => false);

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

        AssertNoLogEntryContains(LogLevel.Error, "An exception occurred while executing query");
    }

    [Test]
    public async Task GivenConfiguredJsonSerializerSettings_QueryIsSerializedWithConfiguredSettingsPreExecution()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"payload\":10}");
    }

    [Test]
    public async Task GivenConfiguredJsonSerializerSettings_ResponseIsSerializedWithConfiguredSettingsPostExecution()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _ = await Handler.ExecuteQuery(testQuery);

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

        _ = await Handler.ExecuteQuery(testQuery);

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

        _ = await Handler.ExecuteQuery(testQuery);

        AssertNoLogEntryContains(LogLevel.None, "Executed query");
    }

    [Test]
    public async Task GivenTraceId_LogsCorrectTraceId()
    {
        var testQuery = new TestQuery(10);

        var traceId = "test-trace-id";

        Resolve<IConquerorContextAccessor>().GetOrCreate().SetTraceId(traceId);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertLogEntryContains(LogLevel.Information, $"Trace ID: {traceId}", nrOfTimes: 2);
    }

    [Test]
    public async Task GivenQueryId_LogsCorrectTraceId()
    {
        var testQuery = new TestQuery(10);

        var queryId = "test-query-id";

        Resolve<IQueryContextAccessor>().SetExternalQueryId(queryId);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertLogEntryContains(LogLevel.Information, $"Query ID: {queryId}", nrOfTimes: 2);
    }

    [Test]
    public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeConfigured()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging().ConfigureLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.ExecuteQuery(testQuery);

        AssertPreExecutionLogMessage(LogLevel.Debug, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeRemoved()
    {
        var testQuery = new TestQuery(10);

        configurePipeline = b => b.UseLogging().WithoutLogging();

        _ = await Handler.ExecuteQuery(testQuery);

        AssertNoLogEntryContains(LogLevel.Debug, "Executing query");
    }

    private void AssertPreExecutionLogMessage(LogLevel logLevel, string? expectedSerializedQuery = null, string? loggerName = null)
    {
        if (expectedSerializedQuery is null)
        {
            var regexWithoutPayload = new Regex(@"Executing query \(Query ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
            AssertLogEntryMatches(logLevel, regexWithoutPayload, loggerName);
            return;
        }

        var regexWithPayload = new Regex(@"Executing query with payload " + Regex.Escape(expectedSerializedQuery) + @" \(Query ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
        AssertLogEntryMatches(logLevel, regexWithPayload, loggerName);
    }

    private void AssertPostExecutionLogMessage(LogLevel logLevel, string? expectedSerializedResponse = null, string? loggerName = null)
    {
        if (expectedSerializedResponse is null)
        {
            var regexWithoutPayload = new Regex(@"Executed query in [0-9.]+ms \(Query ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
            AssertLogEntryMatches(logLevel, regexWithoutPayload, loggerName);
            return;
        }

        var regexWithPayload = new Regex(@"Executed query and got response " + Regex.Escape(expectedSerializedResponse) + @" in [0-9.]+ms \(Query ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
        AssertLogEntryMatches(logLevel, regexWithPayload, loggerName);
    }

    private IQueryHandler<TestQuery, TestQueryResponse> Handler => Resolve<IQueryHandler<TestQuery, TestQueryResponse>>();

    private IQueryHandler<TestQueryWithoutPayload, TestQueryResponse> HandlerWithoutPayload => Resolve<IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSLoggingMiddlewares()
                    .AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(
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
                        pipeline => configurePipeline(pipeline));
    }

    private sealed record TestQuery(int Payload);

    private sealed record TestQueryResponse(int ResponsePayload);

    private sealed record TestQueryWithoutPayload;

    private sealed class ThrowingJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            throw new NotSupportedException();
        }
    }
}
