using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Tests.Signalling;

public static partial class LoggingMiddlewareTestSignals
{
    public enum HookTestBehavior
    {
        HookDoesNotLogAndReturnsFalse,
        HookLogsAndReturnsFalse,
        HookDoesNotLogAndReturnsTrue,
        HookLogsAndReturnsTrue,
    }

    public const string TestTransportName = "test-transport";

    public static void RegisterSignalType<TSignal, TIHandler, THandler>(
        this IServiceCollection services,
        SignalTestCase<TSignal, TIHandler, THandler> testCase)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler
    {
        _ = services.AddSignalHandler<THandler>()
                    .AddSingleton<ISignalTestCasePipelineConfiguration<TSignal>>(testCase);

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
                                  SignalPayloadLoggingStrategy: payloadLoggingStrategy,
                                  responsePayloadLoggingStrategy: payloadLoggingStrategy,
                                  hasCustomCategoryFactory,
                                  hookTestBehavior))
        {
            foreach (var c in GenerateTestCasesWithSettings(t.configuredLogLevel,
                                                            t.preExecutionLogLevel,
                                                            t.postExecutionLogLevel,
                                                            t.exceptionLogLevel,
                                                            t.hasException,
                                                            t.SignalPayloadLoggingStrategy,
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
        yield return new SignalTestCase<TestSignal, TestSignal.IHandler, TestSignalHandler>
        {
            Signal = new() { Payload = 10 },
            SignalJson = "{\"Payload\":10}",
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

        yield return new SignalTestCase<TestSignalWithoutPayload, TestSignalWithoutPayload.IHandler, TestSignalWithoutPayloadHandler>
        {
            Signal = new(),
            SignalJson = null,
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

        yield return new SignalTestCase<TestSignalWithComplexPayload, TestSignalWithComplexPayload.IHandler, TestSignalWithComplexPayloadHandler>
        {
            Signal = new() { Payload = 10, NestedPayload = new() { Payload = 11, Payload2 = 12 } },
            SignalJson = "{\"Payload\":10,\"NestedPayload\":{\"Payload\":11,\"Payload2\":12}}",
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

        yield return new SignalTestCase<TestSignalWithCustomSerializedPayloadType, TestSignalWithCustomSerializedPayloadType.IHandler, TestSignalWithCustomSerializedPayloadTypeHandler>
        {
            Signal = new() { Payload = new(10) },
            SignalJson = "{\"Payload\":10}",
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

        yield return new SignalTestCase<TestSignalWithCustomJsonTypeInfo, TestSignalWithCustomJsonTypeInfo.IHandler, TestSignalWithCustomJsonTypeInfoHandler>
        {
            Signal = new() { SignalPayload = 10 },
            SignalJson = "{\"SIGNAL_PAYLOAD\":10}",
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

        yield return new SignalTestCase<TestSignalWithCustomTransport, TestSignalWithCustomTransport.IHandler, TestSignalWithCustomTransportHandler>
        {
            Signal = new() { Payload = 10 },
            SignalJson = "{\"Payload\":10}",
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

        yield return new SignalTestCase<TestSignalBase, TestSignalBase.IHandler, TestSignalBaseHandler>
        {
            Signal = new TestSignalSub { PayloadBase = 10, PayloadSub = 11 },
            SignalJson = "{\"PayloadSub\":11,\"PayloadBase\":10}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            PayloadLoggingStrategy = payloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            ExpectedLoggerCategory = typeof(TestSignalSub).FullName?.Replace('+', '.')!,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };
    }

    public static void ConfigureLoggingPipeline<TSignal>(ISignalPipeline<TSignal> pipeline,
                                                         ISignalTestCasePipelineConfiguration<TSignal> testCase,
                                                         bool addHooks = true)
        where TSignal : class, ISignal<TSignal>
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
                        ctx.Logger.Log(ctx.LogLevel, "PreHook:{SignalType},{SignalId},{TraceId}", ctx.Signal.GetType().Name, ctx.SignalId, ctx.TraceId);
                    }

                    return hookReturn;
                };

                c.PostExecutionHook = ctx =>
                {
                    if (hookLogs)
                    {
                        ctx.Logger.Log(ctx.LogLevel, "PostHook:{SignalId},{TraceId}", ctx.SignalId, ctx.TraceId);
                    }

                    return hookReturn;
                };

                c.ExceptionHook = ctx =>
                {
                    if (hookLogs)
                    {
                        var exceptionToLog = new WrappingException(ctx.Exception, ctx.ExecutionStackTrace.ToString());
                        ctx.Logger.Log(ctx.LogLevel, exceptionToLog, "ExceptionHook:{ExceptionType},{SignalId},{TraceId}", ctx.Exception.GetType().Name, ctx.SignalId, ctx.TraceId);
                    }

                    return hookReturn;
                };
            }
        });
    }

    private static void ConfigureLoggingPipeline<TSignal>(ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        var testCase = pipeline.ServiceProvider.GetService<ISignalTestCasePipelineConfiguration<TSignal>>();

        if (testCase is not null)
        {
            ConfigureLoggingPipeline(pipeline, testCase);
        }
    }

    public sealed class SignalTestCase<TSignal, TIHandler, THandler> : ISignalTestCasePipelineConfiguration<TSignal>
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
        where THandler : class, ISignalHandler
    {
        private readonly string? expectedLogCategory;

        public required TSignal Signal { get; init; }

        public required string? SignalJson { get; init; }

        public required Exception? Exception { get; init; }

        public required LogLevel ConfiguredLogLevel { get; init; }

        public required LogLevel? PreExecutionLogLevel { get; init; }

        public required LogLevel? PostExecutionLogLevel { get; init; }

        public required LogLevel? ExceptionLogLevel { get; init; }

        public required PayloadLoggingStrategy? PayloadLoggingStrategy { get; init; }

        public required Func<TSignal, string>? LoggerCategoryFactory { get; init; }

        public string ExpectedLoggerCategory
        {
            get => LoggerCategoryFactory?.Invoke(Signal) ?? expectedLogCategory ?? typeof(TSignal).FullName?.Replace('+', '.')!;
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
                var hookSuppressesSignal = HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookDoesNotLogAndReturnsFalse;

                var publisherTransportRole = nameof(SignalTransportRole.Publisher).ToLowerInvariant();
                var receiverTransportRole = nameof(SignalTransportRole.Receiver).ToLowerInvariant();

                if (PreExecutionLogLevel is not LogLevel.None && PreExecutionLogLevel >= ConfiguredLogLevel)
                {
                    var hasPayload = TSignal.EmptyInstance is null;

                    if (!hookSuppressesSignal)
                    {
                        var preExecutionLogRegexBuilder = new StringBuilder();

                        _ = preExecutionLogRegexBuilder.Append("Handling ");

                        if (TransportTypeName is not null)
                        {
                            _ = preExecutionLogRegexBuilder.Append(TransportTypeName).Append(' ');
                        }

                        _ = preExecutionLogRegexBuilder.Append("signal ");

                        if (TransportTypeName is not null)
                        {
                            _ = preExecutionLogRegexBuilder.Append("on ").Append(publisherTransportRole).Append(' ');
                        }

                        if (hasPayload)
                        {
                            if (PayloadLoggingStrategy is Logging.PayloadLoggingStrategy.Raw)
                            {
                                _ = preExecutionLogRegexBuilder.Append($"with payload {Signal} ");
                            }

                            if (PayloadLoggingStrategy is Logging.PayloadLoggingStrategy.IndentedJson)
                            {
                                var json = JsonSerializer.Serialize(JsonDocument.Parse(SignalJson!), new JsonSerializerOptions { WriteIndented = true });
                                _ = preExecutionLogRegexBuilder.Append($"with payload{Environment.NewLine}      {json.Replace(Environment.NewLine, $"{Environment.NewLine}      ")}{Environment.NewLine}      ");
                            }

                            if (PayloadLoggingStrategy is Logging.PayloadLoggingStrategy.MinimalJson or null)
                            {
                                _ = preExecutionLogRegexBuilder.Append($"with payload {SignalJson} ");
                            }
                        }

                        _ = preExecutionLogRegexBuilder.Append(@"\(Signal ID: [a-f0-9]{16}, Trace ID: [a-f0-9]{32}\)");

                        var preExecutionSignal = new Regex(preExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                        yield return (PreExecutionLogLevel ?? LogLevel.Information, preExecutionSignal);

                        if (TransportTypeName is not null)
                        {
                            _ = preExecutionLogRegexBuilder.Replace(publisherTransportRole, receiverTransportRole);

                            var preExecutionServerSignal = new Regex(preExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                            yield return (PreExecutionLogLevel ?? LogLevel.Information, preExecutionServerSignal);
                        }
                    }

                    if (HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookLogsAndReturnsTrue)
                    {
                        var preExecutionSignalFromHook = new Regex($"PreHook:{Signal.GetType().Name},[a-f0-9]{{16}},[a-f0-9]{{32}}");
                        yield return (PreExecutionLogLevel ?? LogLevel.Information, preExecutionSignalFromHook);
                    }
                }

                if (Exception is null && PostExecutionLogLevel is not LogLevel.None && PostExecutionLogLevel >= ConfiguredLogLevel)
                {
                    if (!hookSuppressesSignal)
                    {
                        var postExecutionLogRegexBuilder = new StringBuilder();

                        _ = postExecutionLogRegexBuilder.Append("Handled ");

                        if (TransportTypeName is not null)
                        {
                            _ = postExecutionLogRegexBuilder.Append(TransportTypeName).Append(' ');
                        }

                        _ = postExecutionLogRegexBuilder.Append("signal ");

                        if (TransportTypeName is not null)
                        {
                            _ = postExecutionLogRegexBuilder.Append("on ").Append(publisherTransportRole).Append(' ');
                        }

                        _ = postExecutionLogRegexBuilder.Append(@"in [0-9]+\.[0-9]+ms \(Signal ID: [a-f0-9]{16}, Trace ID: [a-f0-9]{32}\)");

                        var postExecutionSignal = new Regex(postExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                        yield return (PostExecutionLogLevel ?? LogLevel.Information, postExecutionSignal);

                        if (TransportTypeName is not null)
                        {
                            _ = postExecutionLogRegexBuilder.Replace(publisherTransportRole, receiverTransportRole);

                            var postExecutionServerSignal = new Regex(postExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                            yield return (PreExecutionLogLevel ?? LogLevel.Information, postExecutionServerSignal);
                        }
                    }

                    if (HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookLogsAndReturnsTrue)
                    {
                        var postExecutionSignalFromHook = new Regex("PostHook:[a-f0-9]{16},[a-f0-9]{32}");
                        yield return (PostExecutionLogLevel ?? LogLevel.Information, postExecutionSignalFromHook);
                    }
                }

                if (Exception is not null && ExceptionLogLevel is not LogLevel.None && ExceptionLogLevel >= ConfiguredLogLevel)
                {
                    if (!hookSuppressesSignal)
                    {
                        var exceptionLogRegexBuilder = new StringBuilder();

                        _ = exceptionLogRegexBuilder.Append("An exception occurred while handling ");

                        if (TransportTypeName is not null)
                        {
                            _ = exceptionLogRegexBuilder.Append(TransportTypeName).Append(' ');
                        }

                        _ = exceptionLogRegexBuilder.Append("signal ");

                        if (TransportTypeName is not null)
                        {
                            _ = exceptionLogRegexBuilder.Append("on ").Append(publisherTransportRole).Append(' ');
                        }

                        _ = exceptionLogRegexBuilder.Append(@"after [0-9]+\.[0-9]+ms \(Signal ID: [a-f0-9]{16}, Trace ID: [a-f0-9]{32}\)");

                        _ = exceptionLogRegexBuilder.Append(".*test exception");

                        var exceptionSignal = new Regex(exceptionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                        yield return (ExceptionLogLevel ?? LogLevel.Error, exceptionSignal);

                        if (TransportTypeName is not null)
                        {
                            _ = exceptionLogRegexBuilder.Replace(publisherTransportRole, receiverTransportRole);

                            var exceptionServerSignal = new Regex(exceptionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                            yield return (ExceptionLogLevel ?? LogLevel.Error, exceptionServerSignal);
                        }
                    }

                    if (HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookLogsAndReturnsTrue)
                    {
                        var exceptionSignalFromHook = new Regex($"ExceptionHook:{Exception.GetType().Name},[a-f0-9]{{16}},[a-f0-9]{{32}}");
                        yield return (ExceptionLogLevel ?? LogLevel.Error, exceptionSignalFromHook);
                    }
                }
            }
        }

        public JsonSerializerContext? JsonSerializerContext => TSignal.JsonSerializerContext;

        public string TestLabelShort => new StringBuilder().Append(typeof(TSignal).Name)
                                                           .Append($",{ConfiguredLogLevel}")
                                                           .Append($",{PreExecutionLogLevel?.ToString() ?? "Default"}")
                                                           .Append($",{PayloadLoggingStrategy?.ToString() ?? "Default"}")
                                                           .Append($",{LoggerCategoryFactory is not null}")
                                                           .Append($",{Exception is not null}")
                                                           .Append($",{HookBehavior?.ToString() ?? "None"}")
                                                           .ToString();

        private string TestLabel => new StringBuilder().Append(typeof(TSignal).Name)
                                                       .Append($",conf lvl:{ConfiguredLogLevel}")
                                                       .Append($",logged lvl:{PreExecutionLogLevel?.ToString() ?? "Default"}")
                                                       .Append($",strategy:{PayloadLoggingStrategy?.ToString() ?? "Default"}")
                                                       .Append($",has cat:{LoggerCategoryFactory is not null}")
                                                       .Append($",has ex:{Exception is not null}")
                                                       .Append($",hook:{HookBehavior?.ToString() ?? "None"}")
                                                       .ToString();

        public static implicit operator TestCaseData(SignalTestCase<TSignal, TIHandler, THandler> signalTestCase)
        {
            return new(signalTestCase)
            {
                TestName = signalTestCase.TestLabel,
                TypeArgs = [typeof(TSignal), typeof(TIHandler), typeof(THandler)],
            };
        }
    }

    public interface ISignalTestCasePipelineConfiguration
    {
        LogLevel? PreExecutionLogLevel { get; }

        LogLevel? PostExecutionLogLevel { get; }

        LogLevel? ExceptionLogLevel { get; }

        PayloadLoggingStrategy? PayloadLoggingStrategy { get; }

        HookTestBehavior? HookBehavior { get; }
    }

    public interface ISignalTestCasePipelineConfiguration<in TSignal> : ISignalTestCasePipelineConfiguration
        where TSignal : class, ISignal<TSignal>
    {
        Func<TSignal, string>? LoggerCategoryFactory { get; }
    }

    [Signal]
    public sealed partial record TestSignal
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestSignalHandler(Exception? exception = null) : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    [Signal]
    public sealed partial record TestSignalWithoutPayload;

    public sealed partial class TestSignalWithoutPayloadHandler(Exception? exception = null) : TestSignalWithoutPayload.IHandler
    {
        public async Task Handle(TestSignalWithoutPayload signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    [Signal]
    public sealed partial record TestSignalWithComplexPayload
    {
        public required int? Payload { get; init; }

        public required TestSignalWithComplexPayloadPayload NestedPayload { get; init; }
    }

    public sealed record TestSignalWithComplexPayloadPayload
    {
        [Required]
        public required int? Payload { get; init; }

        [Required]
        public required int? Payload2 { get; init; }
    }

    public sealed partial class TestSignalWithComplexPayloadHandler(Exception? exception = null) : TestSignalWithComplexPayload.IHandler
    {
        public async Task Handle(TestSignalWithComplexPayload signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    [Signal]
    public sealed partial record TestSignalWithCustomSerializedPayloadType
    {
        public required TestSignalWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    [JsonConverter(typeof(TestSignalWithCustomSerializedPayloadTypeHandler.PayloadJsonConverter))]
    public sealed record TestSignalWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed partial class TestSignalWithCustomSerializedPayloadTypeHandler(Exception? exception = null) : TestSignalWithCustomSerializedPayloadType.IHandler
    {
        public async Task Handle(TestSignalWithCustomSerializedPayloadType signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
            => ConfigureLoggingPipeline(pipeline);

        internal sealed class PayloadJsonConverter : JsonConverter<TestSignalWithCustomSerializedPayloadTypePayload>
        {
            public override TestSignalWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, TestSignalWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Payload);
            }
        }
    }

    [Signal]
    public sealed partial record TestSignalWithCustomJsonTypeInfo
    {
        public int SignalPayload { get; init; }
    }

    public sealed partial class TestSignalWithCustomJsonTypeInfoHandler(Exception? exception = null) : TestSignalWithCustomJsonTypeInfo.IHandler
    {
        public async Task Handle(TestSignalWithCustomJsonTypeInfo signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
    [JsonSerializable(typeof(TestSignalWithCustomJsonTypeInfo))]
    internal sealed partial class TestSignalWithCustomJsonTypeInfoJsonSerializerContext : JsonSerializerContext;

    [TestTransportSignal]
    public sealed partial record TestSignalWithCustomTransport
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestSignalWithCustomTransportHandler(Exception? exception = null) : TestSignalWithCustomTransport.IHandler
    {
        public async Task Handle(TestSignalWithCustomTransport signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    public sealed class TestSignalPublisher<TSignal> : ISignalPublisher<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public string TransportTypeName => TestTransportName;

        public async Task Publish(TSignal signal,
                                  IServiceProvider serviceProvider,
                                  ConquerorContext conquerorContext,
                                  CancellationToken cancellationToken)
        {
            await Task.Yield();

            var invokers = serviceProvider.GetRequiredService<ISignalHandlerRegistry>()
                                          .GetReceiverHandlerInvokers<ITestTransportSignalHandlerTypesInjector>();

            foreach (var invoker in invokers)
            {
                await invoker.Invoke(signal, serviceProvider, TransportTypeName, null, [], cancellationToken);
            }
        }
    }

    [Signal]
    public partial record TestSignalBase
    {
        public int PayloadBase { get; init; }
    }

    public sealed record TestSignalSub : TestSignalBase
    {
        public int PayloadSub { get; init; }
    }

    public sealed partial class TestSignalBaseHandler(Exception? exception = null) : TestSignalBase.IHandler
    {
        public async Task Handle(TestSignalBase signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
            => ConfigureLoggingPipeline(pipeline);
    }

    public sealed class TestException() : Exception("test exception");

    public sealed class TestSignalBroadcastingStrategy : ISignalBroadcastingStrategy
    {
        public async Task BroadcastSignal<TSignal>(IReadOnlyCollection<SignalHandlerFn<TSignal>> signalHandlerInvocationFns,
                                                   IServiceProvider serviceProvider,
                                                   TSignal signal,
                                                   CancellationToken cancellationToken)
            where TSignal : class, ISignal<TSignal>
        {
            await Task.Yield();

            foreach (var invoker in signalHandlerInvocationFns)
            {
                await invoker.Invoke(signal, serviceProvider, cancellationToken);
            }
        }
    }
}
