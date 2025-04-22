using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Debugging;
using Serilog.Extensions.Logging;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using static Conqueror.Middleware.Logging.Tests.Signalling.LoggingMiddlewareTestSignals;
using Throws = NUnit.Framework.Throws;

namespace Conqueror.Middleware.Logging.Tests.Signalling;

[TestFixture]
public sealed class LoggingSignalMiddlewareTests
{
    private const string TestSignalId = "test-message-id";

    private const string TestTraceId = "test-trace-id";

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddleware_WhenCallingHandler_CorrectMessagesGetLogged<TSignal, THandlerInterface, THandler>(
        SignalTestCase<TSignal, THandlerInterface, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where THandlerInterface : class, IGeneratedSignalHandler<TSignal, THandlerInterface>
        where THandler : class, IGeneratedSignalHandler
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterSignalType(testCase),
            logging =>
            {
                _ = logging.AddTestLogger()

                           // log to console during local development for easier debugging
                           .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                           .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                           .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(logging.Services.Single(s => s.ServiceType == typeof(ILoggerProvider)
                                                                             && s.ImplementationType == typeof(ConsoleLoggerProvider)));
                }
            });

        var handler = host.Resolve<ISignalPublishers>().For<TSignal, THandlerInterface>();

        if (typeof(TSignal) == typeof(TestSignalWithCustomTransport))
        {
            handler = handler.WithPipeline(p => ConfigureLoggingPipeline(p, testCase, addHooks: false))
                             .WithPublisher(b => new TestSignalPublisher<TSignal>(b.UseInProcess(new TestSignalBroadcastingStrategy(TestTransportName))));
        }

        try
        {
            await THandlerInterface.Invoke(handler, testCase.Signal, host.TestTimeoutToken);

            if (testCase.Exception != null)
            {
                Assert.Fail("should have thrown exception");
            }
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        var expectedLogEntries = testCase.ExpectedLogMessages.Select(t => (testCase.ExpectedLoggerCategory, t.LogLevel, t.MessagePattern));

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

        // ReSharper disable once DisposeOnUsingVariable (intentionally done to force a flush)
        await host.DisposeAsync();

        Assert.Multiple(() =>
        {
            foreach (var (cat, lvl, messagePattern) in expectedLogEntries)
            {
                Assert.That(logEntries, Has.Exactly(1).Matches<(string Cat, LogLevel Lvl, string Msg)>(e => e.Cat == cat && e.Lvl == lvl && messagePattern.IsMatch(e.Msg)),
                            $"expected cat={cat}, lvl={lvl}, message pattern={messagePattern}");
            }
        });
    }

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndSimpleLogger_WhenCallingHandler_CorrectMessagesGetLogged<TSignal, THandlerInterface, THandler>(
        SignalTestCase<TSignal, THandlerInterface, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where THandlerInterface : class, IGeneratedSignalHandler<TSignal, THandlerInterface>
        where THandler : class, IGeneratedSignalHandler
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services =>
            {
                services.RegisterSignalType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<ISignalIdFactory, TestSignalIdFactory>());
            },
            logging =>
            {
                _ = logging.AddTestLogger()

                           // log to console during local development for easier debugging
                           .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                           .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                           .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(logging.Services.Single(s => s.ServiceType == typeof(ILoggerProvider)
                                                                             && s.ImplementationType == typeof(ConsoleLoggerProvider)));
                }
            });

        var handler = host.Resolve<ISignalPublishers>().For<TSignal, THandlerInterface>();

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() => TimeSpan.FromMilliseconds(123.456));

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(TestTraceId);

        try
        {
            await THandlerInterface.Invoke(handler, testCase.Signal, host.TestTimeoutToken);
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;
        var logOutput = string.Join(string.Empty, logEntries.Select(e => e.Message));

        _ = await Verify(logOutput.NormalizeLogOutput(), CreateVerifySettings("MicrosoftSimple", testCase.TestLabelShort));
    }

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndJsonLogger_WhenCallingHandler_CorrectMessagesGetLogged<TSignal, THandlerInterface, THandler>(
        SignalTestCase<TSignal, THandlerInterface, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where THandlerInterface : class, IGeneratedSignalHandler<TSignal, THandlerInterface>
        where THandler : class, IGeneratedSignalHandler
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services =>
            {
                services.RegisterSignalType(testCase);
                _ = services.Replace(ServiceDescriptor.Transient<ISignalIdFactory, TestSignalIdFactory>());
            },
            logging =>
            {
                _ = logging.AddTestLogger()

                           // log to console during local development for easier debugging
                           .AddJsonConsole()
                           .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                           .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(logging.Services.Single(s => s.ServiceType == typeof(ILoggerProvider)
                                                                             && s.ImplementationType == typeof(ConsoleLoggerProvider)));
                }
            });

        var handler = host.Resolve<ISignalPublishers>().For<TSignal, THandlerInterface>();

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() => TimeSpan.FromMilliseconds(123.456));

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(TestTraceId);

        try
        {
            await THandlerInterface.Invoke(handler, testCase.Signal, host.TestTimeoutToken);
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;
        var logOutput = string.Join(string.Empty, logEntries.Select(e => e.Message));

        _ = await Verify(logOutput.NormalizeLogOutput(), CreateVerifySettings("MicrosoftJson", testCase.TestLabelShort));
    }

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndSerilogLogger_WhenCallingHandler_CorrectMessagesGetLogged<TSignal, THandlerInterface, THandler>(
        SignalTestCase<TSignal, THandlerInterface, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where THandlerInterface : class, IGeneratedSignalHandler<TSignal, THandlerInterface>
        where THandler : class, IGeneratedSignalHandler
    {
        SelfLog.Enable(Console.Error);

        await using var defaultWriter = new StringWriter();

        var timestamp = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var loggerConfiguration = new LoggerConfiguration().WriteTo.TestSink(timestamp, defaultWriter)

                                                           // for debugging also write to console
                                                           .WriteTo.Conditional(_ => !IsRunningInGithubAction, sinks => sinks.Console())
                                                           .MinimumLevel.Is(LevelConvert.ToSerilogLevel(testCase.ConfiguredLogLevel))
                                                           .Filter.ByExcluding(Matching.FromSource("Microsoft.Extensions.Hosting.Internal.Host"))
                                                           .Filter.ByExcluding(Matching.FromSource("Microsoft.Hosting.Lifetime"))
                                                           .Enrich.With();

        if (testCase.ConfiguredLogLevel == LogLevel.None)
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
            logging => logging.AddSerilog(serilogLogger));

        var handler = host.Resolve<ISignalPublishers>().For<TSignal, THandlerInterface>();

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() => TimeSpan.FromMilliseconds(123.456));

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(TestTraceId);

        try
        {
            await THandlerInterface.Invoke(handler, testCase.Signal, host.TestTimeoutToken);
        }
        catch
        {
            // we are testing a side-effect, so it is fine to ignore everything here
        }

        // ReSharper disable once DisposeOnUsingVariable (intentionally done to force a flush)
        await serilogLogger.DisposeAsync();

        _ = await Verify(defaultWriter.ToString().NormalizeLogOutput(), CreateVerifySettings("Serilog", testCase.TestLabelShort));
    }

    [Test]
    [TestCaseSource(typeof(LoggingMiddlewareTestSignals), nameof(GenerateSnapshotTestCaseData))]
    public async Task GivenHandlerWithLoggingMiddlewareAndSerilogJsonLogger_WhenCallingHandler_CorrectMessagesGetLogged<TSignal, THandlerInterface, THandler>(
        SignalTestCase<TSignal, THandlerInterface, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where THandlerInterface : class, IGeneratedSignalHandler<TSignal, THandlerInterface>
        where THandler : class, IGeneratedSignalHandler
    {
        SelfLog.Enable(Console.Error);

        var jsonFormatter = new RenderedCompactJsonFormatter();

        await using var jsonWriter = new StringWriter();

        var timestamp = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var loggerConfiguration = new LoggerConfiguration().WriteTo.TestSink(timestamp, jsonFormatter, jsonWriter)

                                                           // for debugging also write to console
                                                           .WriteTo.Conditional(_ => !IsRunningInGithubAction, sinks => sinks.Console(jsonFormatter))
                                                           .MinimumLevel.Is(LevelConvert.ToSerilogLevel(testCase.ConfiguredLogLevel))
                                                           .Filter.ByExcluding(Matching.FromSource("Microsoft.Extensions.Hosting.Internal.Host"))
                                                           .Filter.ByExcluding(Matching.FromSource("Microsoft.Hosting.Lifetime"));

        if (testCase.ConfiguredLogLevel == LogLevel.None)
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
            logging => logging.AddSerilog(serilogLogger));

        var handler = host.Resolve<ISignalPublishers>().For<TSignal, THandlerInterface>();

        using var loggingStopWatch = LoggingStopwatch.WithTimingFactory(() => TimeSpan.FromMilliseconds(123.456));

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(TestTraceId);

        try
        {
            await THandlerInterface.Invoke(handler, testCase.Signal, host.TestTimeoutToken);
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
            Signal = new() { Payload = 10 },
            SignalJson = "{\"Payload\":10}",
            Exception = exception,
            ConfiguredLogLevel = LogLevel.Information,
            PreExecutionLogLevel = null,
            PostExecutionLogLevel = null,
            ExceptionLogLevel = null,
            PayloadLoggingStrategy = null,
            LoggerCategoryFactory = null,
            HookBehavior = HookTestBehavior.HookLogsAndReturnsTrue,
            TransportTypeName = null,
        };

        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterSignalType(testCase),
            logging =>
            {
                _ = logging.AddTestLogger(shouldTruncate: false)

                           // log to console during local development for easier debugging
                           .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                           .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                           .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(logging.Services.Single(s => s.ServiceType == typeof(ILoggerProvider)
                                                                             && s.ImplementationType == typeof(ConsoleLoggerProvider)));
                }
            });

        var handler = host.Resolve<ISignalPublishers>()
                          .For(TestSignal.T)
                          .WithPipeline(p => p.UseLogging());

        try
        {
            await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);
            Assert.Fail("Exception should have been thrown");
        }
        catch (TestException thrownException)
        {
            if (!IsRunningInGithubAction)
            {
                await Console.Error.WriteLineAsync(thrownException.ToString());
            }

            var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

            // three matches: server, hook on server, client
            Assert.That(logEntries, Has.Exactly(3).Matches<(string Cat, LogLevel Lvl, string Msg)>(e => e.Lvl == LogLevel.Error && e.Msg.Contains(nameof(GivenHandlerWithLoggingMiddleware_WhenHandlerThrows_ExceptionGetsLoggedWithFullStackTrace))));

            var numberOfTimesExceptionMessageContainsStack = thrownException.ToString()
                                                                            .Split([nameof(GivenHandlerWithLoggingMiddleware_WhenHandlerThrows_ExceptionGetsLoggedWithFullStackTrace)],
                                                                                   StringSplitOptions.None).Length - 1;

            Assert.That(numberOfTimesExceptionMessageContainsStack, Is.EqualTo(1));
        }
    }

    [Test]
    [Combinatorial]
    public async Task GivenHandlerWithLoggingMiddleware_WhenLoggingHookThrows_ThatFailureIsLoggedSeparatelyAndExecutionProceedsNormally(
        [Values("pre", "post", "exception")] string hookThrowLocation)
    {
        var hookException = new TestException();
        var handlerException = new Exception("from handler");

        var testCase = new SignalTestCase<TestSignal, TestSignal.IHandler, TestSignalHandler>
        {
            Signal = new() { Payload = 10 },
            SignalJson = "{\"Payload\":10}",
            Exception = hookThrowLocation is "exception" ? handlerException : null,
            ConfiguredLogLevel = LogLevel.Information,
            PreExecutionLogLevel = null,
            PostExecutionLogLevel = null,
            ExceptionLogLevel = null,
            PayloadLoggingStrategy = null,
            LoggerCategoryFactory = null,
            HookBehavior = HookTestBehavior.HookLogsAndReturnsTrue,
            TransportTypeName = null,
        };

        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.RegisterSignalType(testCase),
            logging =>
            {
                _ = logging.AddTestLogger()

                           // log to console during local development for easier debugging
                           .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                           .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                           .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(logging.Services.Single(s => s.ServiceType == typeof(ILoggerProvider)
                                                                             && s.ImplementationType == typeof(ConsoleLoggerProvider)));
                }
            });

        var handler = host.Resolve<ISignalPublishers>()
                          .For(TestSignal.T)
                          .WithPipeline(p => p.UseLogging(o =>
                          {
                              if (hookThrowLocation is "pre")
                              {
                                  o.PreExecutionHook = _ => throw hookException;
                              }

                              if (hookThrowLocation is "post")
                              {
                                  o.PostExecutionHook = _ => throw hookException;
                              }

                              if (hookThrowLocation is "exception")
                              {
                                  o.ExceptionHook = _ => throw hookException;
                              }
                          }));

        if (hookThrowLocation is "exception")
        {
            var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(testCase.Signal, host.TestTimeoutToken));

            if (!IsRunningInGithubAction)
            {
                await Console.Error.WriteLineAsync(thrownException.ToString());
            }

            Assert.That(thrownException, Is.SameAs(handlerException));
        }
        else
        {
            await Assert.ThatAsync(() => handler.Handle(testCase.Signal, host.TestTimeoutToken), Throws.Nothing);
        }

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

        Assert.That(logEntries, Has.Exactly(1).Matches<(string Cat, LogLevel Lvl, string Msg)>(e => e.Lvl == LogLevel.Error && e.Msg.Contains("An exception occurred while executing logging hook")));
    }

    [Test]
    public async Task GivenHandlerWithLoggingMiddleware_WhenConfiguringLogging_MiddlewareConfigurationGetsUpdated()
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.AddSignalHandler<TestSignalHandler>(),
            logging =>
            {
                _ = logging.AddTestLogger(shouldTruncate: false)

                           // log to console during local development for easier debugging
                           .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                           .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                           .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(logging.Services.Single(s => s.ServiceType == typeof(ILoggerProvider)
                                                                             && s.ImplementationType == typeof(ConsoleLoggerProvider)));
                }
            });

        var handler = host.Resolve<ISignalPublishers>()
                          .For(TestSignal.T)
                          .WithPipeline(p => p.UseLogging(c => c.PreExecutionLogLevel = LogLevel.Warning)
                                              .ConfigureLogging(c => c.PreExecutionLogLevel = LogLevel.Debug));

        await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);

        var logEntries = host.Resolve<LoggingMiddlewareTestLogSink>().LogEntries;

        Assert.That(logEntries, Has.Exactly(1).Matches<(string Cat, LogLevel Lvl, string Msg)>(e => e.Lvl == LogLevel.Debug && e.Msg.Contains("{\"Payload\":10}")));
    }

    [Test]
    public async Task GivenHandlerWithLoggingMiddleware_WhenRemovingMiddlewareFromPipeline_MiddlewareDoesNotGetCalled()
    {
        await using var host = await LoggingMiddlewareTestHost.Create(
            services => services.AddSignalHandler<TestSignalHandler>(),
            logging =>
            {
                _ = logging.AddTestLogger(shouldTruncate: false)

                           // log to console during local development for easier debugging
                           .AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled)
                           .AddFilter("Microsoft.Extensions.Hosting.Internal.Host", _ => false)
                           .AddFilter("Microsoft.Hosting.Lifetime", _ => false);

                if (IsRunningInGithubAction)
                {
                    _ = logging.Services.Remove(logging.Services.Single(s => s.ServiceType == typeof(ILoggerProvider)
                                                                             && s.ImplementationType == typeof(ConsoleLoggerProvider)));
                }
            });

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
        settings.UseFileName($"{testLoggerType}={testCaseLabel.Replace(':', '=').Replace(' ', '_')}");
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }

    private bool IsRunningInGithubAction => Environment.GetEnvironmentVariable("GITHUB_ACTION") is not null;

    private sealed class TestSignalIdFactory : ISignalIdFactory
    {
        public string GenerateId() => TestSignalId;
    }
}
