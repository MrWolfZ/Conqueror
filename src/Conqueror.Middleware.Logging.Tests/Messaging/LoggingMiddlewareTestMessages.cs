using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Tests.Messaging;

[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Members are used by ASP.NET Core via reflection")]
public static partial class LoggingMiddlewareTestMessages
{
    public enum HookTestBehavior
    {
        HookDoesNotLogAndReturnsFalse,
        HookLogsAndReturnsFalse,
        HookDoesNotLogAndReturnsTrue,
        HookLogsAndReturnsTrue,
    }

    public const string TestTransportName = "test-transport";

    public static void RegisterMessageType<TMessage, TResponse, THandler>(this IServiceCollection services,
                                                                          MessageTestCase<TMessage, TResponse, THandler> testCase)
        where TMessage : class, IMessage<TMessage, TResponse>
        where THandler : class, IGeneratedMessageHandler
    {
        _ = services.AddMessageHandler<THandler>()
                    .AddSingleton<IMessageTestCasePipelineConfiguration<TMessage, TResponse>>(testCase);

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
                                  messagePayloadLoggingStrategy: payloadLoggingStrategy,
                                  responsePayloadLoggingStrategy: payloadLoggingStrategy,
                                  hasCustomCategoryFactory,
                                  hookTestBehavior))
        {
            foreach (var c in GenerateTestCasesWithSettings(t.configuredLogLevel,
                                                            t.preExecutionLogLevel,
                                                            t.postExecutionLogLevel,
                                                            t.exceptionLogLevel,
                                                            t.hasException,
                                                            t.messagePayloadLoggingStrategy,
                                                            t.responsePayloadLoggingStrategy,
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
        PayloadLoggingStrategy? messagePayloadLoggingStrategy,
        PayloadLoggingStrategy? responsePayloadLoggingStrategy,
        bool hasCustomCategoryFactory,
        HookTestBehavior? hookTestBehavior)
    {
        yield return new MessageTestCase<TestMessage, TestMessageResponse, TestMessageHandler>
        {
            Message = new() { Payload = 10 },
            MessageJson = "{\"Payload\":10}",
            Response = new() { Payload = 11 },
            ResponseJson = "{\"Payload\":11}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            MessagePayloadLoggingStrategy = messagePayloadLoggingStrategy,
            ResponsePayloadLoggingStrategy = responsePayloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new MessageTestCase<TestMessageWithoutResponse, UnitMessageResponse, TestMessageWithoutResponseHandler>
        {
            Message = new() { Payload = 10 },
            MessageJson = "{\"Payload\":10}",
            Response = null,
            ResponseJson = null,
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            MessagePayloadLoggingStrategy = messagePayloadLoggingStrategy,
            ResponsePayloadLoggingStrategy = responsePayloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new MessageTestCase<TestMessageWithoutPayload, TestMessageResponse, TestMessageWithoutPayloadHandler>
        {
            Message = new(),
            MessageJson = null,
            Response = new() { Payload = 11 },
            ResponseJson = "{\"Payload\":11}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            MessagePayloadLoggingStrategy = messagePayloadLoggingStrategy,
            ResponsePayloadLoggingStrategy = responsePayloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new MessageTestCase<TestMessageWithoutResponseWithoutPayload, UnitMessageResponse, TestMessageWithoutResponseWithoutPayloadHandler>
        {
            Message = new(),
            MessageJson = null,
            Response = null,
            ResponseJson = null,
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            MessagePayloadLoggingStrategy = messagePayloadLoggingStrategy,
            ResponsePayloadLoggingStrategy = responsePayloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new MessageTestCase<TestMessageWithComplexPayload, TestMessageResponse, TestMessageWithComplexPayloadHandler>
        {
            Message = new() { Payload = 10, NestedPayload = new() { Payload = 11, Payload2 = 12 } },
            MessageJson = "{\"Payload\":10,\"NestedPayload\":{\"Payload\":11,\"Payload2\":12}}",
            Response = new() { Payload = 33 },
            ResponseJson = "{\"Payload\":33}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            MessagePayloadLoggingStrategy = messagePayloadLoggingStrategy,
            ResponsePayloadLoggingStrategy = responsePayloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new MessageTestCase<TestMessageWithCustomSerializedPayloadType, TestMessageWithCustomSerializedPayloadTypeResponse, TestMessageWithCustomSerializedPayloadTypeHandler>
        {
            Message = new() { Payload = new(10) },
            MessageJson = "{\"Payload\":10}",
            Response = new() { Payload = new(11) },
            ResponseJson = "{\"Payload\":11}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            MessagePayloadLoggingStrategy = messagePayloadLoggingStrategy,
            ResponsePayloadLoggingStrategy = responsePayloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new MessageTestCase<TestMessageWithCustomJsonTypeInfo, TestMessageWithCustomJsonTypeInfoResponse, TestMessageWithCustomJsonTypeInfoHandler>
        {
            Message = new() { MessagePayload = 10 },
            MessageJson = "{\"MESSAGE_PAYLOAD\":10}",
            Response = new() { ResponsePayload = 11 },
            ResponseJson = "{\"RESPONSE_PAYLOAD\":11}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            MessagePayloadLoggingStrategy = messagePayloadLoggingStrategy,
            ResponsePayloadLoggingStrategy = responsePayloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };

        yield return new MessageTestCase<TestMessageWithCustomTransport, TestMessageResponse, TestMessageWithCustomTransportHandler>
        {
            Message = new() { Payload = 10 },
            MessageJson = "{\"Payload\":10}",
            Response = new() { Payload = 11 },
            ResponseJson = "{\"Payload\":11}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            MessagePayloadLoggingStrategy = messagePayloadLoggingStrategy,
            ResponsePayloadLoggingStrategy = responsePayloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            HookBehavior = hookTestBehavior,
            TransportTypeName = TestTransportName,
        };

        yield return new MessageTestCase<TestMessageBase, TestMessageResponse, TestMessageBaseHandler>
        {
            Message = new TestMessageSub { PayloadBase = 10, PayloadSub = 11 },
            MessageJson = "{\"PayloadSub\":11,\"PayloadBase\":10}",
            Response = new() { Payload = 11 },
            ResponseJson = "{\"Payload\":11}",
            Exception = hasException ? new TestException() : null,
            ConfiguredLogLevel = configuredLogLevel,
            PreExecutionLogLevel = preExecutionLogLevel,
            PostExecutionLogLevel = postExecutionLogLevel,
            ExceptionLogLevel = exceptionLogLevel,
            MessagePayloadLoggingStrategy = messagePayloadLoggingStrategy,
            ResponsePayloadLoggingStrategy = responsePayloadLoggingStrategy,
            LoggerCategoryFactory = hasCustomCategoryFactory ? m => $"CustomCategory_{m.GetType().Name}" : null,
            ExpectedLoggerCategory = typeof(TestMessageSub).FullName?.Replace('+', '.')!,
            HookBehavior = hookTestBehavior,
            TransportTypeName = null,
        };
    }

    public static void ConfigureLoggingPipeline<TMessage, TResponse>(IMessagePipeline<TMessage, TResponse> pipeline,
                                                                     IMessageTestCasePipelineConfiguration<TMessage, TResponse> testCase,
                                                                     bool addHooks = true)
        where TMessage : class, IMessage<TMessage, TResponse>
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

            if (testCase.MessagePayloadLoggingStrategy is not null)
            {
                c.MessagePayloadLoggingStrategy = testCase.MessagePayloadLoggingStrategy.Value;
            }

            if (testCase.ResponsePayloadLoggingStrategy is not null)
            {
                c.ResponsePayloadLoggingStrategy = testCase.ResponsePayloadLoggingStrategy.Value;
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
                        ctx.Logger.Log(ctx.LogLevel, "PreHook:{MessageType},{MessageId},{TraceId}", ctx.Message.GetType().Name, ctx.MessageId, ctx.TraceId);
                    }

                    return hookReturn;
                };

                c.PostExecutionHook = ctx =>
                {
                    if (hookLogs)
                    {
                        ctx.Logger.Log(ctx.LogLevel, "PostHook:{ResponseType},{MessageId},{TraceId}", ctx.Response?.GetType().Name, ctx.MessageId, ctx.TraceId);
                    }

                    return hookReturn;
                };

                c.ExceptionHook = ctx =>
                {
                    if (hookLogs)
                    {
                        var exceptionToLog = new WrappingException(ctx.Exception, ctx.ExecutionStackTrace.ToString());
                        ctx.Logger.Log(ctx.LogLevel, exceptionToLog, "ExceptionHook:{ExceptionType},{MessageId},{TraceId}", ctx.Exception.GetType().Name, ctx.MessageId, ctx.TraceId);
                    }

                    return hookReturn;
                };
            }
        });
    }

    private static void ConfigureLoggingPipeline<TMessage, TResponse>(IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var testCase = pipeline.ServiceProvider.GetService<IMessageTestCasePipelineConfiguration<TMessage, TResponse>>();

        if (testCase is not null)
        {
            ConfigureLoggingPipeline(pipeline, testCase);
        }
    }

    public sealed class MessageTestCase<TMessage, TResponse, THandler> : IMessageTestCasePipelineConfiguration<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
        where THandler : class, IGeneratedMessageHandler
    {
        private readonly string? expectedLogCategory;

        public required TMessage Message { get; init; }

        public required string? MessageJson { get; init; }

        public required TResponse? Response { get; init; }

        public required string? ResponseJson { get; init; }

        public required Exception? Exception { get; init; }

        public required LogLevel ConfiguredLogLevel { get; init; }

        public required LogLevel? PreExecutionLogLevel { get; init; }

        public required LogLevel? PostExecutionLogLevel { get; init; }

        public required LogLevel? ExceptionLogLevel { get; init; }

        public required PayloadLoggingStrategy? MessagePayloadLoggingStrategy { get; init; }

        public required PayloadLoggingStrategy? ResponsePayloadLoggingStrategy { get; init; }

        public required Func<TMessage, string>? LoggerCategoryFactory { get; init; }

        public string ExpectedLoggerCategory
        {
            get => LoggerCategoryFactory?.Invoke(Message) ?? expectedLogCategory ?? typeof(TMessage).FullName?.Replace('+', '.')!;
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
                var hookSuppressesMessage = HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookDoesNotLogAndReturnsFalse;

                var clientTransportRole = nameof(MessageTransportRole.Client).ToLowerInvariant();
                var serverTransportRole = nameof(MessageTransportRole.Server).ToLowerInvariant();

                if (PreExecutionLogLevel is not LogLevel.None && PreExecutionLogLevel >= ConfiguredLogLevel)
                {
                    var hasPayload = TMessage.EmptyInstance is null;

                    if (!hookSuppressesMessage)
                    {
                        var preExecutionLogRegexBuilder = new StringBuilder();

                        _ = preExecutionLogRegexBuilder.Append("Handling ");

                        if (TransportTypeName is not null)
                        {
                            _ = preExecutionLogRegexBuilder.Append(TransportTypeName).Append(' ');
                        }

                        _ = preExecutionLogRegexBuilder.Append("message ");

                        if (TransportTypeName is not null)
                        {
                            _ = preExecutionLogRegexBuilder.Append("on ").Append(clientTransportRole).Append(' ');
                        }

                        if (hasPayload)
                        {
                            if (MessagePayloadLoggingStrategy is PayloadLoggingStrategy.Raw)
                            {
                                _ = preExecutionLogRegexBuilder.Append($"with payload {Message} ");
                            }

                            if (MessagePayloadLoggingStrategy is PayloadLoggingStrategy.IndentedJson)
                            {
                                var json = JsonSerializer.Serialize(JsonDocument.Parse(MessageJson!), new JsonSerializerOptions { WriteIndented = true });
                                _ = preExecutionLogRegexBuilder.Append($"with payload{Environment.NewLine}      {json.Replace(Environment.NewLine, $"{Environment.NewLine}      ")}{Environment.NewLine}      ");
                            }

                            if (MessagePayloadLoggingStrategy is PayloadLoggingStrategy.MinimalJson or null)
                            {
                                _ = preExecutionLogRegexBuilder.Append($"with payload {MessageJson} ");
                            }
                        }

                        _ = preExecutionLogRegexBuilder.Append(@"\(Message ID: [a-f0-9]{16}, Trace ID: [a-f0-9]{32}\)");

                        var preExecutionMessage = new Regex(preExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                        yield return (PreExecutionLogLevel ?? LogLevel.Information, preExecutionMessage);

                        if (TransportTypeName is not null)
                        {
                            _ = preExecutionLogRegexBuilder.Replace(clientTransportRole, serverTransportRole);

                            var preExecutionServerMessage = new Regex(preExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                            yield return (PreExecutionLogLevel ?? LogLevel.Information, preExecutionServerMessage);
                        }
                    }

                    if (HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookLogsAndReturnsTrue)
                    {
                        var preExecutionMessageFromHook = new Regex($"PreHook:{Message.GetType().Name},[a-f0-9]{{16}},[a-f0-9]{{32}}");
                        yield return (PreExecutionLogLevel ?? LogLevel.Information, preExecutionMessageFromHook);
                    }
                }

                if (Exception is null && PostExecutionLogLevel is not LogLevel.None && PostExecutionLogLevel >= ConfiguredLogLevel)
                {
                    var hasResponse = Response is not null;

                    if (!hookSuppressesMessage)
                    {
                        var postExecutionLogRegexBuilder = new StringBuilder();

                        _ = postExecutionLogRegexBuilder.Append("Handled ");

                        if (TransportTypeName is not null)
                        {
                            _ = postExecutionLogRegexBuilder.Append(TransportTypeName).Append(' ');
                        }

                        _ = postExecutionLogRegexBuilder.Append("message ");

                        if (TransportTypeName is not null)
                        {
                            _ = postExecutionLogRegexBuilder.Append("on ").Append(clientTransportRole).Append(' ');
                        }

                        if (hasResponse)
                        {
                            if (MessagePayloadLoggingStrategy is PayloadLoggingStrategy.Raw)
                            {
                                _ = postExecutionLogRegexBuilder.Append($"and got response {Response} ");
                            }

                            if (MessagePayloadLoggingStrategy is PayloadLoggingStrategy.IndentedJson)
                            {
                                var json = JsonSerializer.Serialize(JsonDocument.Parse(ResponseJson!), new JsonSerializerOptions { WriteIndented = true });
                                _ = postExecutionLogRegexBuilder.Append($"and got response{Environment.NewLine}      {json.Replace(Environment.NewLine, $"{Environment.NewLine}      ")}{Environment.NewLine}      ");
                            }

                            if (MessagePayloadLoggingStrategy is PayloadLoggingStrategy.MinimalJson or null)
                            {
                                _ = postExecutionLogRegexBuilder.Append($"and got response {ResponseJson} ");
                            }
                        }

                        _ = postExecutionLogRegexBuilder.Append(@"in [0-9]+\.[0-9]+ms \(Message ID: [a-f0-9]{16}, Trace ID: [a-f0-9]{32}\)");

                        var postExecutionMessage = new Regex(postExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                        yield return (PostExecutionLogLevel ?? LogLevel.Information, postExecutionMessage);

                        if (TransportTypeName is not null)
                        {
                            _ = postExecutionLogRegexBuilder.Replace(clientTransportRole, serverTransportRole);

                            var postExecutionServerMessage = new Regex(postExecutionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                            yield return (PreExecutionLogLevel ?? LogLevel.Information, postExecutionServerMessage);
                        }
                    }

                    if (HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookLogsAndReturnsTrue)
                    {
                        var postExecutionMessageFromHook = new Regex($"PostHook:{typeof(TResponse).Name},[a-f0-9]{{16}},[a-f0-9]{{32}}");
                        yield return (PostExecutionLogLevel ?? LogLevel.Information, postExecutionMessageFromHook);
                    }
                }

                if (Exception is not null && ExceptionLogLevel is not LogLevel.None && ExceptionLogLevel >= ConfiguredLogLevel)
                {
                    if (!hookSuppressesMessage)
                    {
                        var exceptionLogRegexBuilder = new StringBuilder();

                        _ = exceptionLogRegexBuilder.Append("An exception occurred while handling ");

                        if (TransportTypeName is not null)
                        {
                            _ = exceptionLogRegexBuilder.Append(TransportTypeName).Append(' ');
                        }

                        _ = exceptionLogRegexBuilder.Append("message ");

                        if (TransportTypeName is not null)
                        {
                            _ = exceptionLogRegexBuilder.Append("on ").Append(clientTransportRole).Append(' ');
                        }

                        _ = exceptionLogRegexBuilder.Append(@"after [0-9]+\.[0-9]+ms \(Message ID: [a-f0-9]{16}, Trace ID: [a-f0-9]{32}\)");

                        _ = exceptionLogRegexBuilder.Append(".*test exception");

                        var exceptionMessage = new Regex(exceptionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                        yield return (ExceptionLogLevel ?? LogLevel.Error, exceptionMessage);

                        if (TransportTypeName is not null)
                        {
                            _ = exceptionLogRegexBuilder.Replace(clientTransportRole, serverTransportRole);

                            var exceptionServerMessage = new Regex(exceptionLogRegexBuilder.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);

                            yield return (ExceptionLogLevel ?? LogLevel.Error, exceptionServerMessage);
                        }
                    }

                    if (HookBehavior is HookTestBehavior.HookLogsAndReturnsFalse or HookTestBehavior.HookLogsAndReturnsTrue)
                    {
                        var exceptionMessageFromHook = new Regex($"ExceptionHook:{Exception.GetType().Name},[a-f0-9]{{16}},[a-f0-9]{{32}}");
                        yield return (ExceptionLogLevel ?? LogLevel.Error, exceptionMessageFromHook);
                    }
                }
            }
        }

        public JsonSerializerContext? JsonSerializerContext => TMessage.JsonSerializerContext;

        public string TestLabelShort => new StringBuilder().Append(typeof(TMessage).Name)
                                                      .Append($",{ConfiguredLogLevel}")
                                                      .Append($",{PreExecutionLogLevel?.ToString() ?? "Default"}")
                                                      .Append($",{MessagePayloadLoggingStrategy?.ToString() ?? "Default"}")
                                                      .Append($",{LoggerCategoryFactory is not null}")
                                                      .Append($",{Exception is not null}")
                                                      .Append($",{HookBehavior?.ToString() ?? "None"}")
                                                      .ToString();

        private string TestLabel => new StringBuilder().Append(typeof(TMessage).Name)
                                                       .Append($",conf lvl:{ConfiguredLogLevel}")
                                                       .Append($",logged lvl:{PreExecutionLogLevel?.ToString() ?? "Default"}")
                                                       .Append($",strategy:{MessagePayloadLoggingStrategy?.ToString() ?? "Default"}")
                                                       .Append($",has cat:{LoggerCategoryFactory is not null}")
                                                       .Append($",has ex:{Exception is not null}")
                                                       .Append($",hook:{HookBehavior?.ToString() ?? "None"}")
                                                       .ToString();

        public static implicit operator TestCaseData(MessageTestCase<TMessage, TResponse, THandler> messageTestCase)
        {
            return new(messageTestCase)
            {
                TestName = messageTestCase.TestLabel,
                TypeArgs = [typeof(TMessage), typeof(TResponse), typeof(THandler)],
            };
        }
    }

    public interface IMessageTestCasePipelineConfiguration
    {
        LogLevel? PreExecutionLogLevel { get; }

        LogLevel? PostExecutionLogLevel { get; }

        LogLevel? ExceptionLogLevel { get; }

        PayloadLoggingStrategy? MessagePayloadLoggingStrategy { get; }

        PayloadLoggingStrategy? ResponsePayloadLoggingStrategy { get; }

        HookTestBehavior? HookBehavior { get; }
    }

    public interface IMessageTestCasePipelineConfiguration<in TMessage, TResponse> : IMessageTestCasePipelineConfiguration
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        Func<TMessage, string>? LoggerCategoryFactory { get; }
    }

    [Message<TestMessageResponse>]
    public sealed partial record TestMessage
    {
        public int Payload { get; init; }
    }

    public sealed record TestMessageResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageHandler(Exception? exception = null) : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) => ConfigureLoggingPipeline(pipeline);
    }

    [Message<TestMessageResponse>]
    public sealed partial record TestMessageWithoutPayload;

    public sealed class TestMessageWithoutPayloadHandler(Exception? exception = null) : TestMessageWithoutPayload.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithoutPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            return new() { Payload = 11 };
        }

        public static void ConfigurePipeline(TestMessageWithoutPayload.IPipeline pipeline) => ConfigureLoggingPipeline(pipeline);
    }

    [Message]
    public sealed partial record TestMessageWithoutResponse
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithoutResponseHandler(Exception? exception = null) : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline(TestMessageWithoutResponse.IPipeline pipeline) => ConfigureLoggingPipeline(pipeline);
    }

    [Message]
    public sealed partial record TestMessageWithoutResponseWithoutPayload;

    public sealed class TestMessageWithoutResponseWithoutPayloadHandler(Exception? exception = null) : TestMessageWithoutResponseWithoutPayload.IHandler
    {
        public async Task Handle(TestMessageWithoutResponseWithoutPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }
        }

        public static void ConfigurePipeline(TestMessageWithoutResponseWithoutPayload.IPipeline pipeline) => ConfigureLoggingPipeline(pipeline);
    }

    [Message<TestMessageResponse>]
    public sealed partial record TestMessageWithComplexPayload
    {
        public required int? Payload { get; init; }

        public required TestMessageWithComplexPayloadPayload NestedPayload { get; init; }
    }

    public sealed record TestMessageWithComplexPayloadPayload
    {
        [Required]
        public required int? Payload { get; init; }

        [Required]
        public required int? Payload2 { get; init; }
    }

    public sealed class TestMessageWithComplexPayloadHandler(Exception? exception = null) : TestMessageWithComplexPayload.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithComplexPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            return new() { Payload = (message.Payload ?? 0) + (message.NestedPayload.Payload ?? 0) + (message.NestedPayload.Payload2 ?? 0) };
        }

        public static void ConfigurePipeline(TestMessageWithComplexPayload.IPipeline pipeline) => ConfigureLoggingPipeline(pipeline);
    }

    [Message<TestMessageWithCustomSerializedPayloadTypeResponse>]
    public sealed partial record TestMessageWithCustomSerializedPayloadType
    {
        public required TestMessageWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    public sealed record TestMessageWithCustomSerializedPayloadTypeResponse
    {
        public required TestMessageWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    [JsonConverter(typeof(TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverter))]
    public sealed record TestMessageWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed class TestMessageWithCustomSerializedPayloadTypeHandler(Exception? exception = null) : TestMessageWithCustomSerializedPayloadType.IHandler
    {
        public async Task<TestMessageWithCustomSerializedPayloadTypeResponse> Handle(TestMessageWithCustomSerializedPayloadType query,
                                                                                     CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            return new() { Payload = new(query.Payload.Payload + 1) };
        }

        public static void ConfigurePipeline(TestMessageWithCustomSerializedPayloadType.IPipeline pipeline) => ConfigureLoggingPipeline(pipeline);

        internal sealed class PayloadJsonConverter : JsonConverter<TestMessageWithCustomSerializedPayloadTypePayload>
        {
            public override TestMessageWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, TestMessageWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Payload);
            }
        }
    }

    [Message<TestMessageWithCustomJsonTypeInfoResponse>]
    public sealed partial record TestMessageWithCustomJsonTypeInfo
    {
        public int MessagePayload { get; init; }

        // TODO: remove once this gets discovered automatically
        public static JsonSerializerContext JsonSerializerContext => TestMessageWithCustomJsonTypeInfoJsonSerializerContext.Default;
    }

    public sealed record TestMessageWithCustomJsonTypeInfoResponse
    {
        public int ResponsePayload { get; init; }
    }

    public sealed class TestMessageWithCustomJsonTypeInfoHandler(Exception? exception = null) : TestMessageWithCustomJsonTypeInfo.IHandler
    {
        public async Task<TestMessageWithCustomJsonTypeInfoResponse> Handle(TestMessageWithCustomJsonTypeInfo message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            return new() { ResponsePayload = message.MessagePayload + 1 };
        }

        public static void ConfigurePipeline(TestMessageWithCustomJsonTypeInfo.IPipeline pipeline) => ConfigureLoggingPipeline(pipeline);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
    [JsonSerializable(typeof(TestMessageWithCustomJsonTypeInfo))]
    [JsonSerializable(typeof(TestMessageWithCustomJsonTypeInfoResponse))]
    internal sealed partial class TestMessageWithCustomJsonTypeInfoJsonSerializerContext : JsonSerializerContext;

    [Message<TestMessageResponse>]
    public sealed partial record TestMessageWithCustomTransport
    {
        public int Payload { get; init; }
    }

    public sealed class TestMessageWithCustomTransportHandler(Exception? exception = null) : TestMessageWithCustomTransport.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageWithCustomTransport message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigurePipeline(TestMessageWithCustomTransport.IPipeline pipeline) => ConfigureLoggingPipeline(pipeline);
    }

    public sealed class TestMessageTransport<TMessage, TResponse>(IMessageTransportClient<TMessage, TResponse> wrapped)
        : IMessageTransportClient<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public string TransportTypeName => TestTransportName;

        public async Task<TResponse> Send(TMessage message,
                                          IServiceProvider serviceProvider,
                                          ConquerorContext conquerorContext,
                                          CancellationToken cancellationToken)
        {
            await Task.Yield();
            return await wrapped.Send(message, serviceProvider, conquerorContext, cancellationToken);
        }
    }

    [Message<TestMessageResponse>]
    public partial record TestMessageBase
    {
        public int PayloadBase { get; init; }
    }

    public sealed record TestMessageSub : TestMessageBase
    {
        public int PayloadSub { get; init; }
    }

    public sealed class TestMessageBaseHandler(Exception? exception = null) : TestMessageBase.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageBase message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (exception is not null)
            {
                throw exception;
            }

            return new() { Payload = message.PayloadBase + 1 };
        }

        public static void ConfigurePipeline(TestMessageBase.IPipeline pipeline) => ConfigureLoggingPipeline(pipeline);
    }

    public sealed class TestException() : Exception("test exception");
}
