namespace Conqueror.Middleware.Logging.Tests.Signalling;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Debugging;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using static LoggingMiddlewareTestSignals;
using Throws = Throws;

[TestFixture]
public sealed class LoggingSignalMiddlewareTests
{
    private const string TestSignalId = "test-message-id";

    private const string TestTraceId = "test-trace-id";

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddleware_WhenCallingHandler_CorrectMessagesGetLogged<
        TSignal,
        TIHandler,
        THandler
    >(SignalTestCase<TSignal, TIHandler, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler, ISignalHandlerWithSourceGeneration
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterSignalType(testCase),
            logging =>
            {
                _ = logging
                    .AddSimpleConsole()
                    .AddTestLogger()
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);
            }
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TIHandler.SignalTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TSignal) == typeof(TestSignalWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestSignalPublisher<TSignal>());
        }

        try
        {
            await TSignal.InvokeHandler(handler, testCase.Signal, host.TestTimeoutToken);

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
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndSimpleLogger_WhenCallingHandler_CorrectMessagesGetLogged<
        TSignal,
        TIHandler,
        THandler
    >(SignalTestCase<TSignal, TIHandler, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler, ISignalHandlerWithSourceGeneration
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services =>
            {
                services.RegisterSignalType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<ISignalIdFactory, TestSignalIdFactory>());
            },
            logging =>
            {
                _ = logging
                    .AddSimpleConsole()
                    .AddTestLogger()
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);
            }
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TIHandler.SignalTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TSignal) == typeof(TestSignalWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestSignalPublisher<TSignal>());
        }

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() =>
            TimeSpan.FromMilliseconds(value: 123.456)
        );

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.TraceId = TestTraceId;

        try
        {
            await TSignal.InvokeHandler(handler, testCase.Signal, host.TestTimeoutToken);
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
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndJsonLogger_WhenCallingHandler_CorrectMessagesGetLogged<
        TSignal,
        TIHandler,
        THandler
    >(SignalTestCase<TSignal, TIHandler, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler, ISignalHandlerWithSourceGeneration
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services =>
            {
                services.RegisterSignalType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<ISignalIdFactory, TestSignalIdFactory>());
            },
            logging =>
            {
                _ = logging
                    .AddJsonConsole()
                    .AddTestLogger()
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);
            }
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TIHandler.SignalTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TSignal) == typeof(TestSignalWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestSignalPublisher<TSignal>());
        }

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() =>
            TimeSpan.FromMilliseconds(value: 123.456)
        );

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.TraceId = TestTraceId;

        try
        {
            await TSignal.InvokeHandler(handler, testCase.Signal, host.TestTimeoutToken);
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
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndSerilogLogger_WhenCallingHandler_CorrectMessagesGetLogged<
        TSignal,
        TIHandler,
        THandler
    >(SignalTestCase<TSignal, TIHandler, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler, ISignalHandlerWithSourceGeneration
    {
        SelfLog.Enable(Console.Error);

        await using var defaultWriter = new StringWriter();

        var timestamp = new DateTimeOffset(year: 2020, month: 1, day: 1, hour: 0, minute: 0, second: 0, TimeSpan.Zero);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.TestSink(timestamp, defaultWriter)
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
                services.RegisterSignalType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<ISignalIdFactory, TestSignalIdFactory>());
            },
            logging => logging.AddSerilog(serilogLogger)
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TIHandler.SignalTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TSignal) == typeof(TestSignalWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestSignalPublisher<TSignal>());
        }

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() =>
            TimeSpan.FromMilliseconds(value: 123.456)
        );

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.TraceId = TestTraceId;

        try
        {
            await TSignal.InvokeHandler(handler, testCase.Signal, host.TestTimeoutToken);
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
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndSerilogJsonLogger_WhenCallingHandler_CorrectMessagesGetLogged<
        TSignal,
        TIHandler,
        THandler
    >(SignalTestCase<TSignal, TIHandler, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler, ISignalHandlerWithSourceGeneration
    {
        SelfLog.Enable(Console.Error);

        var jsonFormatter = new RenderedCompactJsonFormatter();

        await using var jsonWriter = new StringWriter();

        var timestamp = new DateTimeOffset(year: 2020, month: 1, day: 1, hour: 0, minute: 0, second: 0, TimeSpan.Zero);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.TestSink(timestamp, jsonFormatter, jsonWriter)
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
                services.RegisterSignalType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<ISignalIdFactory, TestSignalIdFactory>());
            },
            logging => logging.AddSerilog(serilogLogger)
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TIHandler.SignalTypes)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        if (typeof(TSignal) == typeof(TestSignalWithCustomTransport))
        {
            handler = handler.WithTransport(_ => new TestSignalPublisher<TSignal>());
        }

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() =>
            TimeSpan.FromMilliseconds(value: 123.456)
        );

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.TraceId = TestTraceId;

        try
        {
            await TSignal.InvokeHandler(handler, testCase.Signal, host.TestTimeoutToken);
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        // ReSharper disable once DisposeOnUsingVariable (intentionally done to force a flush)
        await serilogLogger.DisposeAsync();

        var logOutput = jsonWriter.ToString();

        // the rendered compact json formatter adds a property @i that is based on the message
        // template, which is unfortunately not stable across unix and non-unix systems, so we
        // replace the computed hash value with a static string to achieve stability
        var hashPropRegex = new Regex("\"@i\":\"[a-z0-9]+\"");
        logOutput = hashPropRegex.Replace(logOutput, "\"@i\":\"hash\"");

        _ = await Verify(logOutput.NormalizeLogOutput(), CreateVerifySettings("SerilogJson", testCase.TestLabelShort));
    }

    [Test]
    public async Task GivenHandlerWithLoggingMiddleware_WhenHandlerThrows_ExceptionGetsLoggedWithFullStackTrace()
    {
        var exception = new TestException();

        var testCase = new SignalTestCase<TestSignal, TestSignal.IHandler, TestSignalHandler>
        {
            Signal = new TestSignal { Payload = 10 },
            SignalJson = "{\"Payload\":10}",
            Exception = exception,
            StackTraceCaptureIsDisabled = false,
            ConfiguredLogLevel = LogLevel.Information,
            PreExecutionLogLevel = null,
            PostExecutionLogLevel = null,
            ExceptionLogLevel = null,
            PayloadLoggingStrategy = null,
            PayloadLoggingStrategyFromFactory = null,
            LoggerCategoryFactory = null,
            HookBehavior = HookTestBehavior.HookLogsAndReturnsTrue,
            TransportTypeName = null,
        };

        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterSignalType(testCase),
            logging =>
            {
                _ = logging
                    .AddSimpleConsole()
                    .AddTestLogger(shouldTruncate: false)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);
            }
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TestSignal.T)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        try
        {
            await handler.Handle(testCase.Signal, host.TestTimeoutToken);
            Assert.Fail("Exception should have been thrown");
        }
        catch (TestException thrownException)
        {
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

        var testCase = new SignalTestCase<TestSignal, TestSignal.IHandler, TestSignalHandler>
        {
            Signal = new TestSignal { Payload = 10 },
            SignalJson = "{\"Payload\":10}",
            Exception = exception,
            StackTraceCaptureIsDisabled = true,
            ConfiguredLogLevel = LogLevel.Information,
            PreExecutionLogLevel = null,
            PostExecutionLogLevel = null,
            ExceptionLogLevel = null,
            PayloadLoggingStrategy = null,
            PayloadLoggingStrategyFromFactory = null,
            LoggerCategoryFactory = null,
            HookBehavior = HookTestBehavior.HookLogsAndReturnsTrue,
            TransportTypeName = null,
        };

        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterSignalType(testCase),
            logging =>
            {
                _ = logging
                    .AddSimpleConsole()
                    .AddTestLogger(shouldTruncate: false)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);
            }
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TestSignal.T)
            .WithPipeline(p => ConfigureLoggingPipeline(p, testCase));

        try
        {
            await handler.Handle(testCase.Signal, host.TestTimeoutToken);
            Assert.Fail("Exception should have been thrown");
        }
        catch (TestException)
        {
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

        var testCase = new SignalTestCase<TestSignal, TestSignal.IHandler, TestSignalHandler>
        {
            Signal = new TestSignal { Payload = 10 },
            SignalJson = "{\"Payload\":10}",
            Exception = string.Equals(hookThrowLocation, "exception", StringComparison.Ordinal)
                ? handlerException
                : null,
            StackTraceCaptureIsDisabled = false,
            ConfiguredLogLevel = LogLevel.Information,
            PreExecutionLogLevel = null,
            PostExecutionLogLevel = null,
            ExceptionLogLevel = null,
            PayloadLoggingStrategy = null,
            PayloadLoggingStrategyFromFactory = null,
            LoggerCategoryFactory = null,
            HookBehavior = HookTestBehavior.HookLogsAndReturnsTrue,
            TransportTypeName = null,
        };

        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterSignalType(testCase),
            logging =>
            {
                _ = logging
                    .AddSimpleConsole()
                    .AddTestLogger()
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);
            }
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TestSignal.T)
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
                handler.Handle(testCase.Signal, host.TestTimeoutToken)
            );

            Assert.That(thrownException, Is.SameAs(handlerException));
        }
        else
        {
            await Assert.ThatAsync(() => handler.Handle(testCase.Signal, host.TestTimeoutToken), Throws.Nothing);
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
            services => services.AddSignalHandler<TestSignalHandler>(),
            logging =>
            {
                _ = logging
                    .AddSimpleConsole()
                    .AddTestLogger(shouldTruncate: false)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);
            }
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TestSignal.T)
            .WithPipeline(p =>
                p.UseLogging(c => c.PreExecutionLogLevel = LogLevel.Warning)
                    .ConfigureLogging(c => c.PreExecutionLogLevel = LogLevel.Debug)
            );

        await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);

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
            services => services.AddSignalHandler<TestSignalHandler>(),
            logging =>
            {
                _ = logging
                    .AddSimpleConsole()
                    .AddTestLogger(shouldTruncate: false)
                    .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                    .AddFilter("Microsoft.Hosting.Lifetime", _ => false);
            }
        );

        var handler = host.Resolve<ISignalPublishers>()
            .For(TestSignal.T)
            .WithPipeline(p => p.UseLogging().WithoutLogging());

        await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);

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

    private sealed class TestSignalIdFactory : ISignalIdFactory
    {
        public string GenerateId() => TestSignalId;
    }
}
