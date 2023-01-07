using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests
{
    [TestFixture]
    public sealed class QueryLoggingMiddlewareTests : TestBase
    {
        private Func<TestQuery, TestQueryResponse> handlerFn = qry => new(qry.Payload);
        private Action<IQueryPipelineBuilder> configurePipeline = b => b.UseLogging();

        [Test]
        public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsQueryWithPayloadPreExecution()
        {
            var testQuery = new TestQuery(10);

            _ = await Handler.ExecuteQuery(testQuery);

            AssertLogEntry(LogLevel.Information, $"Executing query with payload {{\"Payload\":{testQuery.Payload}}}");
        }

        [Test]
        public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsQueryThatHasNoPayloadPreExecution()
        {
            var testQuery = new TestQueryWithoutPayload();

            _ = await HandlerWithoutPayload.ExecuteQuery(testQuery);

            AssertLogEntry(LogLevel.Information, "Executing query");
        }

        [Test]
        public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsResponseWithPayloadPostExecution()
        {
            var testQuery = new TestQuery(10);

            var response = await Handler.ExecuteQuery(testQuery);

            AssertLogEntryContains(LogLevel.Information, $"Executed query and got response {{\"ResponsePayload\":{response.ResponsePayload}}} in");
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
        }

        [Test]
        public async Task GivenConfiguredPreExecutionLogLevel_LogsQueryWithPayloadPreExecutionAtSpecifiedLevel()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

            _ = await Handler.ExecuteQuery(testQuery);

            AssertLogEntry(LogLevel.Debug, $"Executing query with payload {{\"Payload\":{testQuery.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredPreExecutionLogLevel_LogsQueryThatHasNoPayloadPreExecutionAtSpecifiedLevel()
        {
            var testQuery = new TestQueryWithoutPayload();

            configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

            _ = await HandlerWithoutPayload.ExecuteQuery(testQuery);

            AssertLogEntry(LogLevel.Debug, "Executing query");
        }

        [Test]
        public async Task GivenConfiguredPostExecutionLogLevel_LogsResponseWithPayloadPostExecutionAtSpecifiedLevel()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging(o => o.PostExecutionLogLevel = LogLevel.Debug);

            var response = await Handler.ExecuteQuery(testQuery);

            AssertLogEntryContains(LogLevel.Debug, $"Executed query and got response {{\"ResponsePayload\":{response.ResponsePayload}}} in");
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
        }

        [Test]
        public async Task GivenConfiguredToOmitQueryPayload_LogsQueryWithoutPayloadPreExecution()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedQueryPayload = true);

            _ = await Handler.ExecuteQuery(testQuery);

            AssertLogEntry(LogLevel.Information, "Executing query");
        }

        [Test]
        public async Task GivenConfiguredToOmitQueryPayload_LogsQueryThatHasNoPayloadPreExecution()
        {
            var testQuery = new TestQueryWithoutPayload();

            configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedQueryPayload = true);

            _ = await HandlerWithoutPayload.ExecuteQuery(testQuery);

            AssertLogEntry(LogLevel.Information, "Executing query");
        }

        [Test]
        public async Task GivenConfiguredToOmitResponsePayload_LogsResponseWithoutPayloadPostExecution()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedResponsePayload = true);

            _ = await Handler.ExecuteQuery(testQuery);

            AssertLogEntryContains(LogLevel.Information, "Executed query in");
        }

        [Test]
        public async Task GivenConfiguredLoggerNameFactory_LogsWithCorrectName()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = qry => $"Custom{qry.GetType().Name}");

            _ = await Handler.ExecuteQuery(testQuery);

            AssertLogEntry($"Custom{testQuery.GetType().Name}", LogLevel.Information, $"Executing query with payload {{\"Payload\":{testQuery.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredLoggerNameFactory_WhenFactoryReturnsNull_LogsWithLoggerWithDefaultName()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = _ => null);

            _ = await Handler.ExecuteQuery(testQuery);

            AssertLogEntry(testQuery.GetType().FullName!.Replace("+", "."), LogLevel.Information, $"Executing query with payload {{\"Payload\":{testQuery.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredPreExecutionHook_HookIsCalledWithCorrectParameters()
        {
            var testQuery = new TestQuery(10);
            QueryLoggingPreExecutionContext? seenContext = null;

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
            Assert.AreSame(testQuery, seenContext?.Query);
            Assert.AreSame(scope.ServiceProvider, seenContext?.ServiceProvider);

            AssertLogEntry(testQuery.GetType().FullName!.Replace("+", "."), LogLevel.Critical, "validation");
        }

        [Test]
        public async Task GivenConfiguredPreExecutionHook_WhenHookReturnsFalse_PreExecutionMessageIsNotLogged()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging(o => o.PreExecutionHook = _ => false);

            _ = await Handler.ExecuteQuery(testQuery);

            AssertNoLogEntry(LogLevel.Information, $"Executing query with payload {{\"Payload\":{testQuery.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredPostExecutionHook_HookIsCalledWithCorrectParameters()
        {
            var testQuery = new TestQuery(10);
            QueryLoggingPostExecutionContext? seenContext = null;

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
            Assert.AreSame(testQuery, seenContext?.Query);
            Assert.AreSame(response, seenContext?.Response);
            Assert.IsTrue(seenContext?.ElapsedTime.Ticks > 0);
            Assert.AreSame(scope.ServiceProvider, seenContext?.ServiceProvider);

            AssertLogEntry(testQuery.GetType().FullName!.Replace("+", "."), LogLevel.Critical, "validation");
        }

        [Test]
        public async Task GivenConfiguredPostExecutionHook_WhenHookReturnsFalse_PostExecutionMessageIsNotLogged()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging(o => o.PostExecutionHook = _ => false);

            var response = await Handler.ExecuteQuery(testQuery);

            AssertNoLogEntryContains(LogLevel.Information, $"Executed query and got response {{\"ResponsePayload\":{response.ResponsePayload}}} in");
        }

        [Test]
        public void GivenConfiguredExceptionHook_HookIsCalledWithCorrectParameters()
        {
            var testQuery = new TestQuery(10);
            var exception = new InvalidOperationException("test exception message");
            QueryLoggingExceptionContext? seenContext = null;

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
            Assert.AreSame(testQuery, seenContext?.Query);
            Assert.AreSame(exception, seenContext?.Exception);
            Assert.IsTrue(seenContext?.ElapsedTime.Ticks > 0);
            Assert.AreSame(scope.ServiceProvider, seenContext?.ServiceProvider);

            AssertLogEntry(testQuery.GetType().FullName!.Replace("+", "."), LogLevel.Trace, "validation");
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

            AssertLogEntry(LogLevel.Information, $"Executing query with payload {{\"payload\":{testQuery.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredJsonSerializerSettings_ResponseIsSerializedWithConfiguredSettingsPostExecution()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var response = await Handler.ExecuteQuery(testQuery);

            AssertLogEntryContains(LogLevel.Information, $"Executed query and got response {{\"responsePayload\":{response.ResponsePayload}}} in");
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
        public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeConfigured()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging().ConfigureLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

            _ = await Handler.ExecuteQuery(testQuery);

            AssertLogEntry(LogLevel.Debug, $"Executing query with payload {{\"Payload\":{testQuery.Payload}}}");
        }

        [Test]
        public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeRemoved()
        {
            var testQuery = new TestQuery(10);

            configurePipeline = b => b.UseLogging().WithoutLogging();

            _ = await Handler.ExecuteQuery(testQuery);

            AssertNoLogEntry(LogLevel.Debug, $"Executing query with payload {{\"Payload\":{testQuery.Payload}}}");
        }

        private IQueryHandler<TestQuery, TestQueryResponse> Handler => Resolve<IQueryHandler<TestQuery, TestQueryResponse>>();

        private IQueryHandler<TestQueryWithoutPayload, TestQueryResponse> HandlerWithoutPayload => Resolve<IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>>();

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddConquerorCQS()
                        .AddConquerorCQSLoggingMiddlewares()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryWithoutPayloadHandler>()
                        .AddTransient<Func<TestQuery, TestQueryResponse>>(_ => qry => handlerFn.Invoke(qry))
                        .AddTransient<Action<IQueryPipelineBuilder>>(_ => b => configurePipeline.Invoke(b))
                        .FinalizeConquerorRegistrations();
        }

        private sealed record TestQuery(int Payload);

        private sealed record TestQueryResponse(int ResponsePayload);

        private sealed record TestQueryWithoutPayload;

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
        {
            private readonly Func<TestQuery, TestQueryResponse> handlerFn;

            public TestQueryHandler(Func<TestQuery, TestQueryResponse> handlerFn)
            {
                this.handlerFn = handlerFn;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return handlerFn(query);
            }

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                var configure = pipeline.ServiceProvider.GetRequiredService<Action<IQueryPipelineBuilder>>();
                configure(pipeline);
            }
        }

        private sealed class TestQueryWithoutPayloadHandler : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>, IConfigureQueryPipeline
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return new(0);
            }

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                var configure = pipeline.ServiceProvider.GetRequiredService<Action<IQueryPipelineBuilder>>();
                configure(pipeline);
            }
        }

        private sealed class ThrowingJsonNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                throw new NotSupportedException();
            }
        }
    }
}
