using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Tests.Eventing;

public static partial class LoggingMiddlewareTestEventNotifications
{
    public enum HookTestBehavior
    {
        HookDoesNotLogAndReturnsFalse,
        HookLogsAndReturnsFalse,
        HookDoesNotLogAndReturnsTrue,
        HookLogsAndReturnsTrue,
    }

    public const string TestTransportName = "test-transport";

    public static void RegisterEventNotificationType<TEventNotification, THandlerInterface, THandler>(
        this IServiceCollection services,
        EventNotificationTestCase<TEventNotification, THandlerInterface, THandler> testCase)
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandlerInterface : class, IGeneratedEventNotificationHandler<TEventNotification, THandlerInterface>
        where THandler : class, IGeneratedEventNotificationHandler
    {
        _ = services.AddEventNotificationHandler<THandler>()
                    .AddSingleton<IEventNotificationTestCasePipelineConfiguration<TEventNotification>>(testCase);

        if (testCase.Exception is not null)
        {
            _ = services.AddSingleton(testCase.Exception);
        }
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1102:Query clause should follow previous clause", Justification = "needed for comment")]
    public static IEnumerable<TestCaseData> GenerateTestCaseData()
    {
        LogLevel?[] allLogLevels = [null, ..Enum.GetValues<LogLevel>()];
        PayloadLoggingStrategy?[] allPayloadLoggingStrategies = [null, ..Enum.GetValues<PayloadLoggingStrategy>()];
        HookTestBehavior?[] allHookTestBehaviors = [null, ..Enum.GetValues<HookTestBehavior>()];

        foreach (var t in from configuredLogLevel in allLogLevels
                          where configuredLogLevel is not null
                          from logLevel in allLogLevels

                          // to reduce the number of test cases we only create those combinations that are
                          // where the configured log level and actual log level are close
                          where Math.Abs((int)(logLevel ?? LogLevel.Information) - (int)configuredLogLevel) <= 1
                          let willLog = logLevel != LogLevel.None && logLevel >= configuredLogLevel

                          // to reduce the number of test cases we don't test generate additional cases
                          // when we know that nothing will be logged anyway
                          from hasException in willLog ? new[] { true, false } : [false]
                          from payloadLoggingStrategy in willLog ? allPayloadLoggingStrategies : [null]
                          from hasCustomCategoryFactory in willLog && payloadLoggingStrategy is null ? new[] { true, false } : [false]
                          from hookTestBehavior in willLog && payloadLoggingStrategy is null ? allHookTestBehaviors : [HookTestBehavior.HookLogsAndReturnsTrue]
                          select (configuredLogLevel: (LogLevel)configuredLogLevel,
                                  preExecutionLogLevel: logLevel,
                                  postExecutionLogLevel: logLevel,
                                  exceptionLogLevel: logLevel,
                                  hasException,
                                  EventNotificationPayloadLoggingStrategy: payloadLoggingStrategy,
                                  responsePayloadLoggingStrategy: payloadLoggingStrategy,
                                  hasCustomCategoryFactory,
                                  hookTestBehavior))
        {
            foreach (var c in GenerateTestCasesWithSettings(t.configuredLogLevel,
                                                            t.preExecutionLogLevel,
                                                            t.postExecutionLogLevel,
                                                            t.exceptionLogLevel,
                                                            t.hasException,
                                                            t.EventNotificationPayloadLoggingStrategy,
                                                            t.hasCustomCategoryFactory,
                                                            t.hookTestBehavior))
            {
                yield return c;
            }
        }
    }

    public static IEnumerable<TestCaseData> GenerateSnapshotTestCaseData()
    {
        PayloadLoggingStrategy?[] allPayloadLoggingStrategies = [null, ..Enum.GetValues<PayloadLoggingStrategy>()];

        foreach (var t in from hasException in new[] { true, false }
                          from hasCustomCategoryFactory in new[] { true, false }
                          from payloadLoggingStrategy in allPayloadLoggingStrategies
                          from hookTestBehavior in new HookTestBehavior?[] { HookTestBehavior.HookLogsAndReturnsFalse, null }
                          select (hasException, hasCustomCategoryFactory, payloadLoggingStrategy, hookTestBehavior))
        {
            foreach (var c in GenerateTestCasesWithSettings(configuredLogLevel: LogLevel.Information,
                                                            preExecutionLogLevel: null,
                                                            postExecutionLogLevel: null,
                                                            exceptionLogLevel: null,
                                                            t.hasException,
                                                            t.payloadLoggingStrategy,
                                                            t.hasCustomCategoryFactory,
                                                            t.hookTestBehavior))
            {
                yield return c;
            }
        }
    }

    private static IEnumerable<TestCaseData> GenerateTestCasesWithSettings(
        LogLevel configuredLogLevel,
        LogLevel? preExecutionLogLevel,
        LogLevel? postExecutionLogLevel,
        LogLevel? exceptionLogLevel,
        bool hasException,
        PayloadLoggingStrategy? payloadLoggingStrategy,
        bool hasCustomCategoryFactory,
        HookTestBehavior? hookTestBehavior)
    {
        yield return new EventNotificationTestCase<TestEventNotification, TestEventNotification.IHandler, TestEventNotificationHandler>
        {
            EventNotification = new() { Payload = 10 },
            EventNotificationJson = "{\"Payload\":10}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            PayloadLoggingStrategy = payloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new EventNotificationTestCase<TestEventNotificationWithoutPayload, TestEventNotificationWithoutPayload.IHandler, TestEventNotificationWithoutPayloadHandler>
        {
            EventNotification = new(),
            EventNotificationJson = null,
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            PayloadLoggingStrategy = payloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new EventNotificationTestCase<TestEventNotificationWithComplexPayload, TestEventNotificationWithComplexPayload.IHandler, TestEventNotificationWithComplexPayloadHandler>
        {
            EventNotification = new() { Payload = 10, NestedPayload = new() { Payload = 11, Payload2 = 12 } },
            EventNotificationJson = "{\"Payload\":10,\"NestedPayload\":{\"Payload\":11,\"Payload2\":12}}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            PayloadLoggingStrategy = payloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new EventNotificationTestCase<TestEventNotificationWithCustomSerializedPayloadType, TestEventNotificationWithCustomSerializedPayloadType.IHandler, TestEventNotificationWithCustomSerializedPayloadTypeHandler>
        {
            EventNotification = new() { Payload = new(10) },
            EventNotificationJson = "{\"Payload\":10}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            PayloadLoggingStrategy = payloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new EventNotificationTestCase<TestEventNotificationWithCustomJsonTypeInfo, TestEventNotificationWithCustomJsonTypeInfo.IHandler, TestEventNotificationWithCustomJsonTypeInfoHandler>
        {
            EventNotification = new() { EventNotificationPayload = 10 },
            EventNotificationJson = "{\"EVENT_NOTIFICATION_PAYLOAD\":10}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            PayloadLoggingStrategy = payloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new EventNotificationTestCase<TestEventNotificationWithCustomTransport, TestEventNotificationWithCustomTransport.IHandler, TestEventNotificationWithCustomTransportHandler>
        {
            EventNotification = new() { Payload = 10 },
            EventNotificationJson = "{\"Payload\":10}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            PayloadLoggingStrategy = payloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = TestTransportName,
        };

        yield return new EventNotificationTestCase<TestEventNotificationBase, TestEventNotificationBase.IHandler, TestEventNotificationBaseHandler>
        {
            EventNotification = new TestEventNotificationSub { PayloadBase = 10, PayloadSub = 11 },
            EventNotificationJson = "{\"PayloadSub\":11,\"PayloadBase\":10}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            PayloadLoggingStrategy = payloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            ExpectedLoggerCategory = typeof(TestEventNotificationSub).FullName?.Replace('+', '.')!,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };
    }

    public static void ConfigureLoggingPipeline<TEventNotification>(IEventNotificationPipeline<TEventNotification> pipeline,
                                                                    IEventNotificationTestCasePipelineConfiguration<TEventNotification> testCase,
                                                                    bool addHooks = true)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        _ = pipeline.UseLogging(c =>
        {
            if (testCase.PreExecutionLogLevel is not null)
            {
                c.PreExecutionLogLevel = testCase.PreExecutionLogLevel.Value;
            }

            if (testCase.PostExecutionLogLevel is not null)
            {
                c.PostExecutionLogLevel = testCase.PostExecutionLogLevel.Value;
            }

            if (testCase.ExceptionLogLevel is not null)
            {
                c.ExceptionLogLevel = testCase.ExceptionLogLevel.Value;
            }

            if (testCase.PayloadLoggingStrategy is not null)
            {
                c.PayloadLoggingStrategy = testCase.PayloadLoggingStrategy.Value;
            }

            if (testCase.LoggerCategoryFactory is not null)
            {
                c.LoggerCategoryFactory = testCase.LoggerCategoryFactory;
            }

            if (!addHooks)
            {
                return;
            }

            if (testCase.HookBehavior is { } hookTestBehavior)
            {
                var hookReturn = hookTestBehavior is HookTestBehavior.HookLogsAndReturnsTrue or HookTestBehavior.HookDoesNotLogAndReturnsTrue;
                var hookLogs = hookTestBehavior is HookTestBehavior.HookLogsAndReturnsTrue or HookTestBehavior.HookLogsAndReturnsFalse;

                c.PreExecutionHook = ctx =>
                {
                    if (hookLogs)
                    {
                        ctx.Logger.Log(ctx.LogLevel, "PreHook:{EventNotificationType},{EventNotificationId},{TraceId}", ctx.EventNotification.GetType().Name, ctx.EventNotificationId, ctx.TraceId);
                    }

                    return hookReturn;
                };

                c.PostExecutionHook = ctx =>
                {
                    if (hookLogs)
                    {
                        ctx.Logger.Log(ctx.LogLevel, "PostHook:{EventNotificationId},{TraceId}", ctx.EventNotificationId, ctx.TraceId);
                    }

                    return hookReturn;
                };

                c.ExceptionHook = ctx =>
                {
                    if (hookLogs)
                    {
                        var exceptionToLog = new WrappingException(ctx.Exception, ctx.ExecutionStackTrace.ToString());
                        ctx.Logger.Log(ctx.LogLevel, exceptionToLog, "ExceptionHook:{ExceptionType},{EventNotificationId},{TraceId}", ctx.Exception.GetType().Name, ctx.EventNotificationId, ctx.TraceId);
                    }

                    return hookReturn;
                };
            }
        });
    }

    private static void ConfigureLoggingPipeline<TEventNotification>(IEventNotificationPipeline<TEventNotification> pipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        var testCase = pipeline.ServiceProvider.GetService<IEventNotificationTestCasePipelineConfiguration<TEventNotification>>();

        if (testCase is not null)
        {
            ConfigureLoggingPipeline(pipeline, testCase);
        }
    }

    public sealed class EventNotificationTestCase<TEventNotification, THandlerInterface, THandler> : IEventNotificationTestCasePipelineConfiguration<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
        where THandlerInterface : class, IGeneratedEventNotificationHandler<TEventNotification, THandlerInterface>
        where THandler : class, IGeneratedEventNotificationHandler
    {
        private readonly string? expectedLogCategory;

        public required TEventNotification EventNotification { get; init; }

        public required string? EventNotificationJson { get; init; }

        public required Exception? Exception { get; init; }

        public required LogLevel ConfiguredLogLevel { get; init; }

        public required LogLevel? PreExecutionLogLevel { get; init; }

        public required LogLevel? PostExecutionLogLevel { get; init; }

        public required LogLevel? ExceptionLogLevel { get; init; }

        public required PayloadLoggingStrategy? PayloadLoggingStrategy { get; init; }

        public required Func<TEventNotification, string>? LoggerCategoryFactory { get; init; }

        public string ExpectedLoggerCategory
        {
            get => LoggerCategoryFactory?.Invoke(EventNotification) ?? expectedLogCategory ?? typeof(TEventNotification).FullName?.Replace('+', '.')!;
            init => expectedLogCategory = value;
        }

        public required HookTestBehavior? HookBehavior { get; init; }

        public required string? TransportTypeName { get; init; }

        [SuppressMessage("Performance", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.", Justification = "we don't need the performance here")]
        public IEnumerable<(LogLevel LogLevel, Regex MessagePattern)> ExpectedLogMessages
        {
            [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "it is a well-known value")]
            get
            {
                var hookSuppressesEventNotification = HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookDoesNotLogAndReturnsFalse;

                var publisherTransportRole = nameof(EventNotificationTransportRole.Publisher).ToLowerInvariant();
                var receiverTransportRole = nameof(EventNotificationTransportRole.Receiver).ToLowerInvariant();

                if (PreExecutionLogLevel is not LogLevel.None && PreExecutionLogLevel >= ConfiguredLogLevel)
                {
                    var hasPayload = TEventNotification.EmptyInstance is null;

                    if (!hookSuppressesEventNotification)
                    {
                        var preExecutionLogRegexBuilder = new StringBuilder();

                        _ = preExecutionLogRegexBuilder.Append("Handling ");

                        if (TransportTypeName is not null)
                        {
                            _ = preExecutionLogRegexBuilder.Append(TransportTypeName).Append(' ');
                        }

                        _ = preExecutionLogRegexBuilder.Append("event notification ");

                        if (TransportTypeName is not null)
                        {
                            _ = preExecutionLogRegexBuilder.Append("on ").Append(publisherTransportRole).Append(' ');
                        }

                        if (hasPayload)
                        {
                            if (PayloadLoggingStrategy is Logging.PayloadLoggingStrategy.Raw)
                            {
                                _ = preExecutionLogRegexBuilder.Append($"with payload {EventNotification} ");
                            }

                            if (PayloadLoggingStrategy is Logging.PayloadLoggingStrategy.IndentedJson)
                            {
                                var json = JsonSerializer.Serialize(JsonDocument.Parse(EventNotificationJson!), new JsonSerializerOptions { WriteIndented = true });
                                _ = preExecutionLogRegexBuilder.Append($"with payload{Environment.NewLine}      {json.Replace(Environment.NewLine, $"{Environment.NewLine}      ")}{Environment.NewLine}      ");
                            }

                            if (PayloadLoggingStrategy is Logging.PayloadLoggingStrategy.MinimalJson or null)
                            {
                                _ = preExecutionLogRegexBuilder.Append($"with payload {EventNotificationJson} ");
                            }
                        }

                        _ = preExecutionLogRegexBuilder.Append(@"\(Event Notification ID: [a-f0-9]{16}, Trace ID: [a-f0-9]{32}\)");

                        var preExecutionEventNotification = new Regex(preExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                        yield return (PreExecutionLogLevel ?? LogLevel.Information, preExecutionEventNotification);

                        if (TransportTypeName is not null)
                        {
                            _ = preExecutionLogRegexBuilder.Replace(publisherTransportRole, receiverTransportRole);

                            var preExecutionServerEventNotification = new Regex(preExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                            yield return (PreExecutionLogLevel ?? LogLevel.Information, preExecutionServerEventNotification);
                        }
                    }

                    if (HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookLogsAndReturnsTrue)
                    {
                        var preExecutionEventNotificationFromHook = new Regex($"PreHook:{EventNotification.GetType().Name},[a-f0-9]{{16}},[a-f0-9]{{32}}");
                        yield return (PreExecutionLogLevel ?? LogLevel.Information, preExecutionEventNotificationFromHook);
                    }
                }

                if (Exception is null && PostExecutionLogLevel is not LogLevel.None && PostExecutionLogLevel >= ConfiguredLogLevel)
                {
                    if (!hookSuppressesEventNotification)
                    {
                        var postExecutionLogRegexBuilder = new StringBuilder();

                        _ = postExecutionLogRegexBuilder.Append("Handled ");

                        if (TransportTypeName is not null)
                        {
                            _ = postExecutionLogRegexBuilder.Append(TransportTypeName).Append(' ');
                        }

                        _ = postExecutionLogRegexBuilder.Append("event notification ");

                        if (TransportTypeName is not null)
                        {
                            _ = postExecutionLogRegexBuilder.Append("on ").Append(publisherTransportRole).Append(' ');
                        }

                        _ = postExecutionLogRegexBuilder.Append(@"in [0-9]+\.[0-9]+ms \(Event Notification ID: [a-f0-9]{16}, Trace ID: [a-f0-9]{32}\)");

                        var postExecutionEventNotification = new Regex(postExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                        yield return (PostExecutionLogLevel ?? LogLevel.Information, postExecutionEventNotification);

                        if (TransportTypeName is not null)
                        {
                            _ = postExecutionLogRegexBuilder.Replace(publisherTransportRole, receiverTransportRole);

                            var postExecutionServerEventNotification = new Regex(postExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                            yield return (PreExecutionLogLevel ?? LogLevel.Information, postExecutionServerEventNotification);
                        }
                    }

                    if (HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookLogsAndReturnsTrue)
                    {
                        var postExecutionEventNotificationFromHook = new Regex("PostHook:[a-f0-9]{16},[a-f0-9]{32}");
                        yield return (PostExecutionLogLevel ?? LogLevel.Information, postExecutionEventNotificationFromHook);
                    }
                }

                if (Exception is not null && ExceptionLogLevel is not LogLevel.None && ExceptionLogLevel >= ConfiguredLogLevel)
                {
                    if (!hookSuppressesEventNotification)
                    {
                        var exceptionLogRegexBuilder = new StringBuilder();

                        _ = exceptionLogRegexBuilder.Append("An exception occurred while handling ");

                        if (TransportTypeName is not null)
                        {
                            _ = exceptionLogRegexBuilder.Append(TransportTypeName).Append(' ');
                        }

                        _ = exceptionLogRegexBuilder.Append("event notification ");

                        if (TransportTypeName is not null)
                        {
                            _ = exceptionLogRegexBuilder.Append("on ").Append(publisherTransportRole).Append(' ');
                        }

                        _ = exceptionLogRegexBuilder.Append(@"after [0-9]+\.[0-9]+ms \(Event Notification ID: [a-f0-9]{16}, Trace ID: [a-f0-9]{32}\)");

                        _ = exceptionLogRegexBuilder.Append(".*test exception");

                        var exceptionEventNotification = new Regex(exceptionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                        yield return (ExceptionLogLevel ?? LogLevel.Error, exceptionEventNotification);

                        if (TransportTypeName is not null)
                        {
                            _ = exceptionLogRegexBuilder.Replace(publisherTransportRole, receiverTransportRole);

                            var exceptionServerEventNotification = new Regex(exceptionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                            yield return (ExceptionLogLevel ?? LogLevel.Error, exceptionServerEventNotification);
                        }
                    }

                    if (HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookLogsAndReturnsTrue)
                    {
                        var exceptionEventNotificationFromHook = new Regex($"ExceptionHook:{Exception.GetType().Name},[a-f0-9]{{16}},[a-f0-9]{{32}}");
                        yield return (ExceptionLogLevel ?? LogLevel.Error, exceptionEventNotificationFromHook);
                    }
                }
            }
        }

        public JsonSerializerContext? JsonSerializerContext => TEventNotification.JsonSerializerContext;

        public string TestLabelShort => new StringBuilder().Append(typeof(TEventNotification).Name)
                                                           .Append($",{ConfiguredLogLevel}")
                                                           .Append($",{PreExecutionLogLevel?.ToString() ?? "Default"}")
                                                           .Append($",{PayloadLoggingStrategy?.ToString() ?? "Default"}")
                                                           .Append($",{LoggerCategoryFactory is not null}")
                                                           .Append($",{Exception is not null}")
                                                           .Append($",{HookBehavior?.ToString() ?? "None"}")
                                                           .ToString();

        private string TestLabel => new StringBuilder().Append(typeof(TEventNotification).Name)
                                                       .Append($",conf lvl:{ConfiguredLogLevel}")
                                                       .Append($",logged lvl:{PreExecutionLogLevel?.ToString() ?? "Default"}")
                                                       .Append($",strategy:{PayloadLoggingStrategy?.ToString() ?? "Default"}")
                                                       .Append($",has cat:{LoggerCategoryFactory is not null}")
                                                       .Append($",has ex:{Exception is not null}")
                                                       .Append($",hook:{HookBehavior?.ToString() ?? "None"}")
                                                       .ToString();

        public static implicit operator TestCaseData(EventNotificationTestCase<TEventNotification, THandlerInterface, THandler> notificationTestCase)
        {
            return new(notificationTestCase)
            {
                TestName = notificationTestCase.TestLabel,
                TypeArgs = [typeof(TEventNotification), typeof(THandlerInterface), typeof(THandler)],
            };
        }
    }

    public interface IEventNotificationTestCasePipelineConfiguration
    {
        LogLevel? PreExecutionLogLevel { get; }

        LogLevel? PostExecutionLogLevel { get; }

        LogLevel? ExceptionLogLevel { get; }

        PayloadLoggingStrategy? PayloadLoggingStrategy { get; }

        HookTestBehavior? HookBehavior { get; }
    }

    public interface IEventNotificationTestCasePipelineConfiguration<in TEventNotification> : IEventNotificationTestCasePipelineConfiguration
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        Func<TEventNotification, string>? LoggerCategoryFactory { get; }
    }

    [EventNotification]
    public sealed partial record TestEventNotification
    {
        public int Payload { get; init; }
    }

    public sealed class TestEventNotificationHandler(Exception? exception = null) : TestEventNotification.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    [EventNotification]
    public sealed partial record TestEventNotificationWithoutPayload;

    public sealed class TestEventNotificationWithoutPayloadHandler(Exception? exception = null) : TestEventNotificationWithoutPayload.IHandler
    {
        public async Task Handle(TestEventNotificationWithoutPayload notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    [EventNotification]
    public sealed partial record TestEventNotificationWithComplexPayload
    {
        public required int? Payload { get; init; }

        public required TestEventNotificationWithComplexPayloadPayload NestedPayload { get; init; }
    }

    public sealed record TestEventNotificationWithComplexPayloadPayload
    {
        [Required]
        public required int? Payload { get; init; }

        [Required]
        public required int? Payload2 { get; init; }
    }

    public sealed class TestEventNotificationWithComplexPayloadHandler(Exception? exception = null) : TestEventNotificationWithComplexPayload.IHandler
    {
        public async Task Handle(TestEventNotificationWithComplexPayload notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    [EventNotification]
    public sealed partial record TestEventNotificationWithCustomSerializedPayloadType
    {
        public required TestEventNotificationWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    [JsonConverter(typeof(TestEventNotificationWithCustomSerializedPayloadTypeHandler.PayloadJsonConverter))]
    public sealed record TestEventNotificationWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed class TestEventNotificationWithCustomSerializedPayloadTypeHandler(Exception? exception = null) : TestEventNotificationWithCustomSerializedPayloadType.IHandler
    {
        public async Task Handle(TestEventNotificationWithCustomSerializedPayloadType notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
            => ConfigureLoggingPipeline(pipeline);

        internal sealed class PayloadJsonConverter : JsonConverter<TestEventNotificationWithCustomSerializedPayloadTypePayload>
        {
            public override TestEventNotificationWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, TestEventNotificationWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Payload);
            }
        }
    }

    [EventNotification]
    public sealed partial record TestEventNotificationWithCustomJsonTypeInfo
    {
        public int EventNotificationPayload { get; init; }
    }

    public sealed class TestEventNotificationWithCustomJsonTypeInfoHandler(Exception? exception = null) : TestEventNotificationWithCustomJsonTypeInfo.IHandler
    {
        public async Task Handle(TestEventNotificationWithCustomJsonTypeInfo notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
    [JsonSerializable(typeof(TestEventNotificationWithCustomJsonTypeInfo))]
    internal sealed partial class TestEventNotificationWithCustomJsonTypeInfoJsonSerializerContext : JsonSerializerContext;

    [EventNotification]
    public sealed partial record TestEventNotificationWithCustomTransport
    {
        public int Payload { get; init; }
    }

    public sealed class TestEventNotificationWithCustomTransportHandler(Exception? exception = null) : TestEventNotificationWithCustomTransport.IHandler
    {
        public async Task Handle(TestEventNotificationWithCustomTransport notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    public sealed class TestEventNotificationPublisher<TEventNotification>(IEventNotificationPublisher<TEventNotification> wrapped)
        : IEventNotificationPublisher<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public string TransportTypeName => TestTransportName;

        public async Task Publish(TEventNotification notification,
                                  IServiceProvider serviceProvider,
                                  ConquerorContext conquerorContext,
                                  CancellationToken cancellationToken)
        {
            await Task.Yield();
            await wrapped.Publish(notification, serviceProvider, conquerorContext, cancellationToken);
        }
    }

    [EventNotification]
    public partial record TestEventNotificationBase
    {
        public int PayloadBase { get; init; }
    }

    public sealed record TestEventNotificationSub : TestEventNotificationBase
    {
        public int PayloadSub { get; init; }
    }

    public sealed class TestEventNotificationBaseHandler(Exception? exception = null) : TestEventNotificationBase.IHandler
    {
        public async Task Handle(TestEventNotificationBase notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    public sealed class TestException() : Exception("test exception");

    public sealed class TestEventNotificationBroadcastingStrategy(string transportTypeNameOverride) : IEventNotificationBroadcastingStrategy
    {
        public async Task BroadcastEventNotification<TEventNotification>(IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker> eventNotificationHandlerInvokers,
                                                                         IServiceProvider serviceProvider,
                                                                         TEventNotification notification,
                                                                         string transportTypeName,
                                                                         CancellationToken cancellationToken)
            where TEventNotification : class, IEventNotification<TEventNotification>
        {
            await Task.Yield();

            foreach (var invoker in eventNotificationHandlerInvokers)
            {
                await invoker.Invoke(serviceProvider, notification, transportTypeNameOverride, cancellationToken);
            }
        }
    }
}
