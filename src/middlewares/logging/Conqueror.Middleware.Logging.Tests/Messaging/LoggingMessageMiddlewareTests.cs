namespace Conqueror.Middleware.Logging.Tests.Messaging;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Debugging;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using static LoggingMiddlewareTestMessages;
using Throws = Throws;

[TestFixture]
internal sealed partial class LoggingMessageMiddlewareTests
{
    private const string TestMessageId = "test-message-id";

    private const string TestTraceId = "test-trace-id";

    private bool IsRunningInGithubAction => Environment.GetEnvironmentVariable("GITHUB_ACTION") is not null;

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddleware_WhenCallingHandler_CorrectMessagesGetLogged<
        TMessage,
        TResponse,
        TIHandler,
        THandler
    >(MessageTestCase<TMessage, TResponse, TIHandler, THandler> testCase)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler, IMessageHandlerWithSourceGeneration
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterMessageType(testCase),
            logging =>
            {
                _ = logging
                    .AddTestLogger()
                    // log to console during local development for easier debugging
                    .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(
                        logging.Services.Single(s =>
                            s.ServiceType == typeof(ILoggerProvider)
                            && s.ImplementationType == typeof(ConsoleLoggerProvider)
                        )
                    );
                }
            }
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TIHandler.MessageTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TMessage) == typeof(TestMessageWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestMessageTransport<TMessage, TResponse>());
        }

        try
        {
            _ = await TMessage.InvokeHandler(handler, testCase.Message, host.TestTimeoutToken);

            if (testCase.Exception is not null)
            {
                Assert.Fail("should have thrown exception");
            }
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

        // ReSharper disable once DisposeOnUsingVariable (intentionally done to force a flush)
        await host.DisposeAsync();

        Assert.Multiple(() =>
        {
            foreach (var (cat, lvl, messagePattern) in testCase.ExpectedLogMessages)
            {
                Assert.That(
                    logEntries,
                    Has.Exactly(expectedCount: 1)
                        .Matches<(string Cat, LogLevel Lvl, string Msg)>(e =>
                            string.Equals(e.Cat, cat, StringComparison.Ordinal)
                            && e.Lvl == lvl
                            && messagePattern.IsMatch(e.Msg)
                        ),
                    $"expected cat={cat}, lvl={lvl}, message pattern={messagePattern}"
                );
            }
        });
    }

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestMessages), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndSimpleLogger_WhenCallingHandler_CorrectMessagesGetLogged<
        TMessage,
        TResponse,
        TIHandler,
        THandler
    >(MessageTestCase<TMessage, TResponse, TIHandler, THandler> testCase)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler, IMessageHandlerWithSourceGeneration
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services =>
            {
                services.RegisterMessageType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<IMessageIdFactory, TestMessageIdFactory>());
            },
            logging =>
            {
                _ = logging
                    .AddTestLogger()
                    // log to console during local development for easier debugging
                    .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(
                        logging.Services.Single(s =>
                            s.ServiceType == typeof(ILoggerProvider)
                            && s.ImplementationType == typeof(ConsoleLoggerProvider)
                        )
                    );
                }
            }
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TIHandler.MessageTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TMessage) == typeof(TestMessageWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestMessageTransport<TMessage, TResponse>());
        }

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() =>
            TimeSpan.FromMilliseconds(value: 123.456)
        );

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.TraceId = TestTraceId;

        try
        {
            _ = await TMessage.InvokeHandler(handler, testCase.Message, host.TestTimeoutToken);
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;
        var logOutput = string.Concat(logEntries.Select(e => e.Message));

        _ = await Verify(
            logOutput.NormalizeLogOutput(),
            CreateVerifySettings("MicrosoftSimple", testCase.TestLabelShort)
        );
    }

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestMessages), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndJsonLogger_WhenCallingHandler_CorrectMessagesGetLogged<
        TMessage,
        TResponse,
        TIHandler,
        THandler
    >(MessageTestCase<TMessage, TResponse, TIHandler, THandler> testCase)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler, IMessageHandlerWithSourceGeneration
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services =>
            {
                services.RegisterMessageType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<IMessageIdFactory, TestMessageIdFactory>());
            },
            logging =>
            {
                _ = logging
                    .AddTestLogger()
                    // log to console during local development for easier debugging
                    .AddJsonConsole()
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(
                        logging.Services.Single(s =>
                            s.ServiceType == typeof(ILoggerProvider)
                            && s.ImplementationType == typeof(ConsoleLoggerProvider)
                        )
                    );
                }
            }
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TIHandler.MessageTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TMessage) == typeof(TestMessageWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestMessageTransport<TMessage, TResponse>());
        }

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() =>
            TimeSpan.FromMilliseconds(value: 123.456)
        );

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.TraceId = TestTraceId;

        try
        {
            _ = await TMessage.InvokeHandler(handler, testCase.Message, host.TestTimeoutToken);
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;
        var logOutput = string.Concat(logEntries.Select(e => e.Message));

        _ = await Verify(
            logOutput.NormalizeLogOutput(),
            CreateVerifySettings("MicrosoftJson", testCase.TestLabelShort)
        );
    }

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestMessages), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndSerilogLogger_WhenCallingHandler_CorrectMessagesGetLogged<
        TMessage,
        TResponse,
        TIHandler,
        THandler
    >(MessageTestCase<TMessage, TResponse, TIHandler, THandler> testCase)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler, IMessageHandlerWithSourceGeneration
    {
        SelfLog.Enable(Console.Error);

        await using var defaultWriter = new StringWriter();

        var timestamp = new DateTimeOffset(year: 2020, month: 1, day: 1, hour: 0, minute: 0, second: 0, TimeSpan.Zero);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.TestSink(timestamp, defaultWriter)
            // for debugging also write to console
            .WriteTo.Conditional(_ => !IsRunningInGithubAction, sinks => sinks.Console())
            .MinimumLevel.Is(LevelConvert.ToSerilogLevel(testCase.ConfiguredLogLevel))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.Extensions.Hosting.Internal.Host"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.Hosting.Lifetime"))
            .Enrich.With();

        if (testCase.ConfiguredLogLevel is LogLevel.None)
        {
            loggerConfiguration = loggerConfiguration.Filter.ByExcluding(_ => false);
        }

        await using var serilogLogger = loggerConfiguration.CreateLogger();

        await using var host = await LoggingMiddlewareTestHost.Create(
            services =>
            {
                services.RegisterMessageType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<IMessageIdFactory, TestMessageIdFactory>());
            },
            logging => logging.AddSerilog(serilogLogger)
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TIHandler.MessageTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TMessage) == typeof(TestMessageWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestMessageTransport<TMessage, TResponse>());
        }

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() =>
            TimeSpan.FromMilliseconds(value: 123.456)
        );

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.TraceId = TestTraceId;

        try
        {
            _ = await TMessage.InvokeHandler(handler, testCase.Message, host.TestTimeoutToken);
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        // ReSharper disable once DisposeOnUsingVariable (intentionally done to force a flush)
        await serilogLogger.DisposeAsync();

        _ = await Verify(
            defaultWriter.ToString().NormalizeLogOutput(),
            CreateVerifySettings("Serilog", testCase.TestLabelShort)
        );
    }

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestMessages), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndSerilogJsonLogger_WhenCallingHandler_CorrectMessagesGetLogged<
        TMessage,
        TResponse,
        TIHandler,
        THandler
    >(MessageTestCase<TMessage, TResponse, TIHandler, THandler> testCase)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler, IMessageHandlerWithSourceGeneration
    {
        SelfLog.Enable(Console.Error);

        var jsonFormatter = new RenderedCompactJsonFormatter();

        await using var jsonWriter = new StringWriter();

        var timestamp = new DateTimeOffset(year: 2020, month: 1, day: 1, hour: 0, minute: 0, second: 0, TimeSpan.Zero);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.TestSink(timestamp, jsonFormatter, jsonWriter)
            // for debugging also write to console
            .WriteTo.Conditional(_ => !IsRunningInGithubAction, sinks => sinks.Console(jsonFormatter))
            .MinimumLevel.Is(LevelConvert.ToSerilogLevel(testCase.ConfiguredLogLevel))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.Extensions.Hosting.Internal.Host"))
            .Filter.ByExcluding(Matching.FromSource("Microsoft.Hosting.Lifetime"));

        if (testCase.ConfiguredLogLevel is LogLevel.None)
        {
            loggerConfiguration = loggerConfiguration.Filter.ByExcluding(_ => false);
        }

        await using var serilogLogger = loggerConfiguration.CreateLogger();

        await using var host = await LoggingMiddlewareTestHost.Create(
            services =>
            {
                services.RegisterMessageType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<IMessageIdFactory, TestMessageIdFactory>());
            },
            logging => logging.AddSerilog(serilogLogger)
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TIHandler.MessageTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TMessage) == typeof(TestMessageWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestMessageTransport<TMessage, TResponse>());
        }

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() =>
            TimeSpan.FromMilliseconds(value: 123.456)
        );

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.TraceId = TestTraceId;

        try
        {
            _ = await TMessage.InvokeHandler(handler, testCase.Message, host.TestTimeoutToken);
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        // ReSharper disable once DisposeOnUsingVariable (intentionally done to force a flush)
        await serilogLogger.DisposeAsync();

        var logOutput = jsonWriter.ToString();

        logOutput = ReplaceMessageHashParameterRegex().Replace(logOutput, "\"@i\":\"hash\"");

        _ = await Verify(logOutput.NormalizeLogOutput(), CreateVerifySettings("SerilogJson", testCase.TestLabelShort));
    }

    [Test]
    public async Task GivenHandlerWithLoggingMiddleware_WhenHandlerThrows_ExceptionGetsLoggedWithFullStackTrace()
    {
        var exception = new TestException();

        var testCase = new MessageTestCase<TestMessage, TestMessageResponse, TestMessage.IHandler, TestMessageHandler>
        {
            Message = new TestMessage { Payload = 10 },
            MessageJson = "{\"Payload\":10}",
            Response = new TestMessageResponse { Payload = 11 },
            ResponseJson = "{\"Payload\":11}",
            Exception = exception,
            StackTraceCaptureIsDisabled = false,
            ConfiguredLogLevel = LogLevel.Information,
            PreExecutionLogLevel = null,
            PostExecutionLogLevel = null,
            ExceptionLogLevel = null,
            MessagePayloadLoggingStrategy = null,
            MessagePayloadLoggingStrategyFromFactory = null,
            ResponsePayloadLoggingStrategy = null,
            ResponsePayloadLoggingStrategyFromFactory = null,
            LoggerCategoryFactory = null,
            HookBehavior = HookTestBehavior.HookLogsAndReturnsTrue,
            TransportTypeName = null,
        };

        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterMessageType(testCase),
            logging =>
            {
                _ = logging
                    .AddTestLogger(shouldTruncate: false)
                    // log to console during local development for easier debugging
                    .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(
                        logging.Services.Single(s =>
                            s.ServiceType == typeof(ILoggerProvider)
                            && s.ImplementationType == typeof(ConsoleLoggerProvider)
                        )
                    );
                }
            }
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TestMessage.T)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        try
        {
            _ = await handler.Handle(testCase.Message, host.TestTimeoutToken);
            Assert.Fail("Exception should have been thrown");
        }
        catch (TestException thrownException)
        {
            if (!IsRunningInGithubAction)
            {
                await Console.Error.WriteLineAsync(thrownException.ToString());
            }

            var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

            // four matches: server, hook on server, client, hook on client
            Assert.That(
                logEntries,
                Has.Exactly(expectedCount: 4)
                    .Matches<(string Cat, LogLevel Lvl, string Msg)>(e =>
                        e.Lvl is LogLevel.Error
                        && e.Msg.Contains(
                            nameof(
                                GivenHandlerWithLoggingMiddleware_WhenHandlerThrows_ExceptionGetsLoggedWithFullStackTrace
                            ),
                            StringComparison.Ordinal
                        )
                    )
            );

            var numberOfTimesExceptionMessageContainsStack =
                thrownException
                    .ToString()
                    .Split(
                        [
                            nameof(
                                GivenHandlerWithLoggingMiddleware_WhenHandlerThrows_ExceptionGetsLoggedWithFullStackTrace
                            ),
                        ],
                        StringSplitOptions.None
                    )
                    .Length - 1;

            Assert.That(numberOfTimesExceptionMessageContainsStack, Is.EqualTo(expected: 1));
        }
    }

    [Test]
    public async Task GivenHandlerWithLoggingMiddlewareWithStackTraceCaptureDisabled_WhenHandlerThrows_ExceptionGetsLoggedWithReducedStackTrace()
    {
        var exception = new TestException();

        var testCase = new MessageTestCase<TestMessage, TestMessageResponse, TestMessage.IHandler, TestMessageHandler>
        {
            Message = new TestMessage { Payload = 10 },
            MessageJson = "{\"Payload\":10}",
            Response = new TestMessageResponse { Payload = 11 },
            ResponseJson = "{\"Payload\":11}",
            Exception = exception,
            StackTraceCaptureIsDisabled = true,
            ConfiguredLogLevel = LogLevel.Information,
            PreExecutionLogLevel = null,
            PostExecutionLogLevel = null,
            ExceptionLogLevel = null,
            MessagePayloadLoggingStrategy = null,
            MessagePayloadLoggingStrategyFromFactory = null,
            ResponsePayloadLoggingStrategy = null,
            ResponsePayloadLoggingStrategyFromFactory = null,
            LoggerCategoryFactory = null,
            HookBehavior = HookTestBehavior.HookLogsAndReturnsTrue,
            TransportTypeName = null,
        };

        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterMessageType(testCase),
            logging =>
            {
                _ = logging
                    .AddTestLogger(shouldTruncate: false)
                    // log to console during local development for easier debugging
                    .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(
                        logging.Services.Single(s =>
                            s.ServiceType == typeof(ILoggerProvider)
                            && s.ImplementationType == typeof(ConsoleLoggerProvider)
                        )
                    );
                }
            }
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TestMessage.T)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        try
        {
            _ = await handler.Handle(testCase.Message, host.TestTimeoutToken);
            Assert.Fail("Exception should have been thrown");
        }
        catch (TestException thrownException)
        {
            if (!IsRunningInGithubAction)
            {
                await Console.Error.WriteLineAsync(thrownException.ToString());
            }

            var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

            // the test method should be missing from the stack trace since the caller capture is disabled
            Assert.That(
                logEntries,
                Has.Exactly(expectedCount: 0)
                    .Matches<(string Cat, LogLevel Lvl, string Msg)>(e =>
                        e.Lvl is LogLevel.Error
                        && e.Msg.Contains(
                            nameof(
                                GivenHandlerWithLoggingMiddlewareWithStackTraceCaptureDisabled_WhenHandlerThrows_ExceptionGetsLoggedWithReducedStackTrace
                            ),
                            StringComparison.Ordinal
                        )
                    )
            );
        }
    }

    [Test]
    [Combinatorial]
    public async Task GivenHandlerWithLoggingMiddleware_WhenLoggingHookThrows_ThatFailureIsLoggedSeparatelyAndExecutionProceedsNormally(
        [Values("pre", "post", "exception")] string hookThrowLocation
    )
    {
        var hookException = new TestException();
        var handlerException = new Exception("from handler");

        var testCase = new MessageTestCase<TestMessage, TestMessageResponse, TestMessage.IHandler, TestMessageHandler>
        {
            Message = new TestMessage { Payload = 10 },
            MessageJson = "{\"Payload\":10}",
            Response = new TestMessageResponse { Payload = 11 },
            ResponseJson = "{\"Payload\":11}",
            Exception = string.Equals(hookThrowLocation, "exception", StringComparison.Ordinal)
                ? handlerException
                : null,
            StackTraceCaptureIsDisabled = false,
            ConfiguredLogLevel = LogLevel.Information,
            PreExecutionLogLevel = null,
            PostExecutionLogLevel = null,
            ExceptionLogLevel = null,
            MessagePayloadLoggingStrategy = null,
            MessagePayloadLoggingStrategyFromFactory = null,
            ResponsePayloadLoggingStrategy = null,
            ResponsePayloadLoggingStrategyFromFactory = null,
            LoggerCategoryFactory = null,
            HookBehavior = HookTestBehavior.HookLogsAndReturnsTrue,
            TransportTypeName = null,
        };

        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterMessageType(testCase),
            logging =>
            {
                _ = logging
                    .AddTestLogger()
                    // log to console during local development for easier debugging
                    .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(
                        logging.Services.Single(s =>
                            s.ServiceType == typeof(ILoggerProvider)
                            && s.ImplementationType == typeof(ConsoleLoggerProvider)
                        )
                    );
                }
            }
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TestMessage.T)
            .WithPipeline(p =>
                p.UseLogging(o =>
                {
                    if (string.Equals(hookThrowLocation, "pre", StringComparison.Ordinal))
                    {
                        o.PreExecutionHook = _ => throw hookException;
                    }

                    if (string.Equals(hookThrowLocation, "post", StringComparison.Ordinal))
                    {
                        o.PostExecutionHook = _ => throw hookException;
                    }

                    if (string.Equals(hookThrowLocation, "exception", StringComparison.Ordinal))
                    {
                        o.ExceptionHook = _ => throw hookException;
                    }
                })
            );

        if (string.Equals(hookThrowLocation, "exception", StringComparison.Ordinal))
        {
            var thrownException = Assert.ThrowsAsync<Exception>(() =>
                handler.Handle(testCase.Message, host.TestTimeoutToken)
            );

            if (!IsRunningInGithubAction)
            {
                await Console.Error.WriteLineAsync(thrownException.ToString());
            }

            Assert.That(thrownException, Is.SameAs(handlerException));
        }
        else
        {
            await Assert.ThatAsync(() => handler.Handle(testCase.Message, host.TestTimeoutToken), Throws.Nothing);
        }

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

        Assert.That(
            logEntries,
            Has.Exactly(expectedCount: 1)
                .Matches<(string Cat, LogLevel Lvl, string Msg)>(e =>
                    e.Lvl is LogLevel.Error
                    && e.Msg.Contains("An exception occurred while executing logging hook", StringComparison.Ordinal)
                )
        );
    }

    [Test]
    public async Task GivenHandlerWithLoggingMiddleware_WhenConfiguringLogging_MiddlewareConfigurationGetsUpdated()
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.AddMessageHandler<TestMessageHandler>(),
            logging =>
            {
                _ = logging
                    .AddTestLogger(shouldTruncate: false)
                    // log to console during local development for easier debugging
                    .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(
                        logging.Services.Single(s =>
                            s.ServiceType == typeof(ILoggerProvider)
                            && s.ImplementationType == typeof(ConsoleLoggerProvider)
                        )
                    );
                }
            }
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TestMessage.T)
            .WithPipeline(p =>
                p.UseLogging(c => c.PreExecutionLogLevel = LogLevel.Warning)
                    .ConfigureLogging(c => c.PreExecutionLogLevel = LogLevel.Debug)
            );

        _ = await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

        Assert.That(
            logEntries,
            Has.Exactly(expectedCount: 1)
                .Matches<(string Cat, LogLevel Lvl, string Msg)>(e =>
                    e.Lvl is LogLevel.Debug && e.Msg.Contains("{\"Payload\":10}", StringComparison.Ordinal)
                )
        );
    }

    [Test]
    public async Task GivenHandlerWithLoggingMiddleware_WhenRemovingMiddlewareFromPipeline_MiddlewareDoesNotGetCalled()
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.AddMessageHandler<TestMessageHandler>(),
            logging =>
            {
                _ = logging
                    .AddTestLogger(shouldTruncate: false)
                    // log to console during local development for easier debugging
                    .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(
                        logging.Services.Single(s =>
                            s.ServiceType == typeof(ILoggerProvider)
                            && s.ImplementationType == typeof(ConsoleLoggerProvider)
                        )
                    );
                }
            }
        );

        var handler = host.Resolve<IMessageSenders>()
            .For(TestMessage.T)
            .WithPipeline(p => p.UseLogging().WithoutLogging());

        _ = await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

        Assert.That(logEntries, Is.Empty);
    }

    private static VerifySettings CreateVerifySettings(string testLoggerType, string testCaseLabel)
    {
        var settings = new VerifySettings();
        settings.UseDirectory("Snapshots");
        settings.UseFileName(
            $"{testLoggerType}={testCaseLabel.Replace(oldChar: ':', newChar: '=').Replace(oldChar: ' ', newChar: '_')}"
        );
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }

    // the rendered compact json formatter adds a property @i that is based on the message
    // template, which is unfortunately not stable across unix and non-unix systems, so we
    // replace the computed hash value with a static string to achieve stability
    [GeneratedRegex("\"@i\":\"[a-z0-9]+\"", RegexOptions.None, matchTimeoutMilliseconds: 2000)]
    private static partial Regex ReplaceMessageHashParameterRegex();

    private sealed class TestMessageIdFactory : IMessageIdFactory
    {
        public string GenerateId() => TestMessageId;
    }
}
