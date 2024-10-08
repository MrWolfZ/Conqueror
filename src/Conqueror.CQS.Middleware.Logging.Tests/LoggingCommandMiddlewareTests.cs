using System.Text.Json;
using System.Text.RegularExpressions;
using Conqueror.Common;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests;

[TestFixture]
public sealed class LoggingCommandMiddlewareTests : TestBase
{
    private const string TestTransportTypeName = "test-transport";

    private Func<TestCommand, TestCommandResponse> handlerFn = cmd => new(cmd.Payload);
    private Action<ICommandPipeline<TestCommand, TestCommandResponse>> configurePipeline = b => b.UseLogging();
    private Action<ICommandPipeline<TestCommandWithoutPayload, TestCommandResponse>> configurePipelineWithoutPayload = b => b.UseLogging();
    private Action<ICommandPipeline<TestCommandWithoutResponse>> configurePipelineWithoutResponse = b => b.UseLogging();

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsCommandWithPayloadPreExecution()
    {
        var testCommand = new TestCommand(10);

        _ = await Handler.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsCommandThatHasNoPayloadPreExecution()
    {
        var testCommand = new TestCommandWithoutPayload();

        _ = await HandlerWithoutPayload.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsTransportTypePreExecution()
    {
        var testCommand = new TestCommandWithTransport();

        _ = await Resolve<ICommandHandler<TestCommandWithTransport, TestCommandResponse>>().WithPipeline(p => p.UseLogging()).Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information, null, TestTransportTypeName, CommandTransportRole.Client);
        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", TestTransportTypeName, CommandTransportRole.Server);
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsResponseWithPayloadPostExecution()
    {
        var testCommand = new TestCommand(10);

        _ = await Handler.Handle(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information, "{\"ResponsePayload\":10}");
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsForCommandWithoutResponsePostExecution()
    {
        var testCommand = new TestCommandWithoutResponse(10);

        await HandlerWithoutResponse.Handle(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsCommandWithTransportTypePostExecution()
    {
        var testCommand = new TestCommandWithTransport();

        _ = await Resolve<ICommandHandler<TestCommandWithTransport, TestCommandResponse>>().WithPipeline(p => p.UseLogging(o => o.OmitJsonSerializedResponsePayload = true))
                                                                                     .Handle(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information, null, TestTransportTypeName, CommandTransportRole.Client);
        AssertPostExecutionLogMessage(LogLevel.Information, "{\"ResponsePayload\":10}", TestTransportTypeName, CommandTransportRole.Server);
    }

    [Test]
    public void GivenDefaultLoggingMiddlewareConfiguration_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTrace()
    {
        var testCommand = new TestCommand(10);
        var exception = new InvalidOperationException("test exception message");

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.Handle(testCommand));

        AssertLogEntryContains(LogLevel.Error, "An exception occurred while executing command");
        AssertLogEntryContains(LogLevel.Error, exception.Message);
        AssertLogEntryContains(LogLevel.Error, exception.StackTrace![..exception.StackTrace!.IndexOf($"{nameof(LoggingCommandMiddleware<TestCommand, TestCommandResponse>)}`2.{nameof(LoggingCommandMiddleware<TestCommand, TestCommandResponse>.Execute)}(", StringComparison.Ordinal)]);
        AssertLogEntryContains(LogLevel.Error, $"{nameof(GivenDefaultLoggingMiddlewareConfiguration_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTrace)}()");
        AssertLogEntryContains(LogLevel.Error, "Command ID: ");
        AssertLogEntryContains(LogLevel.Error, "Trace ID: ");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevel_LogsCommandWithPayloadPreExecutionAtSpecifiedLevel()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Debug, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevel_LogsCommandThatHasNoPayloadPreExecutionAtSpecifiedLevel()
    {
        var testCommand = new TestCommandWithoutPayload();

        configurePipelineWithoutPayload = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await HandlerWithoutPayload.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Debug);
    }

    [Test]
    public async Task GivenConfiguredPostExecutionLogLevel_LogsResponseWithPayloadPostExecutionAtSpecifiedLevel()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.PostExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.Handle(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Debug, "{\"ResponsePayload\":10}");
    }

    [Test]
    public async Task GivenConfiguredPostExecutionLogLevel_LogsForCommandWithoutResponsePostExecutionAtSpecifiedLevel()
    {
        var testCommand = new TestCommandWithoutResponse(10);

        configurePipelineWithoutResponse = b => b.UseLogging(o => o.PostExecutionLogLevel = LogLevel.Debug);

        await HandlerWithoutResponse.Handle(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Debug);
    }

    [Test]
    public void GivenConfiguredExceptionLogLevel_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTraceAtSpecifiedLevel()
    {
        var testCommand = new TestCommand(10);
        var exception = new InvalidOperationException("test exception message");

        handlerFn = _ => throw exception;

        configurePipeline = b => b.UseLogging(o => o.ExceptionLogLevel = LogLevel.Critical);

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.Handle(testCommand));

        AssertLogEntryContains(LogLevel.Critical, "An exception occurred while executing command");
        AssertLogEntryContains(LogLevel.Critical, exception.Message);
        AssertLogEntryContains(LogLevel.Critical, exception.StackTrace![..exception.StackTrace!.IndexOf($"{nameof(LoggingCommandMiddleware<TestCommand, TestCommandResponse>)}`2.{nameof(LoggingCommandMiddleware<TestCommand, TestCommandResponse>.Execute)}(", StringComparison.Ordinal)]);
        AssertLogEntryContains(LogLevel.Critical, $"{nameof(GivenConfiguredExceptionLogLevel_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTraceAtSpecifiedLevel)}()");
        AssertLogEntryContains(LogLevel.Critical, "Command ID: ");
        AssertLogEntryContains(LogLevel.Critical, "Trace ID: ");
    }

    [Test]
    public async Task GivenConfiguredToOmitCommandPayload_LogsCommandWithoutPayloadPreExecution()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedCommandPayload = true);

        _ = await Handler.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitCommandPayload_LogsCommandThatHasNoPayloadPreExecution()
    {
        var testCommand = new TestCommandWithoutPayload();

        configurePipelineWithoutPayload = b => b.UseLogging(o => o.OmitJsonSerializedCommandPayload = true);

        _ = await HandlerWithoutPayload.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitResponsePayload_LogsResponseWithoutPayloadPostExecution()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedResponsePayload = true);

        _ = await Handler.Handle(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitResponsePayload_LogsForCommandWithoutResponsePostExecution()
    {
        var testCommand = new TestCommandWithoutResponse(10);

        configurePipelineWithoutResponse = b => b.UseLogging(o => o.OmitJsonSerializedResponsePayload = true);

        await HandlerWithoutResponse.Handle(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredLoggerNameFactory_LogsWithCorrectName()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = cmd => $"Custom{cmd.GetType().Name}");

        _ = await Handler.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", loggerName: $"Custom{testCommand.GetType().Name}");
    }

    [Test]
    public async Task GivenConfiguredLoggerNameFactory_WhenFactoryReturnsNull_LogsWithLoggerWithDefaultName()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = _ => null);

        _ = await Handler.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", loggerName: testCommand.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPreExecutionHook_HookIsCalledWithCorrectParameters()
    {
        var testCommand = new TestCommand(10);
        LoggingCommandPreExecutionContext? seenContext = null;

        var commandId = string.Empty;
        var traceId = "test-trace-id";

        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(traceId);

        using var scope = Host.Services.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        configurePipeline = b => b.UseLogging(o =>
        {
            commandId = b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetCommandId();

            o.PreExecutionLogLevel = LogLevel.Debug;

            o.PreExecutionHook = ctx =>
            {
                seenContext = ctx;
                return true;
            };
        });

        _ = await handler.Handle(testCommand);

        seenContext?.Logger.LogCritical("validation");

        Assert.That(seenContext, Is.Not.Null);
        Assert.That(seenContext?.LogLevel, Is.EqualTo(LogLevel.Debug));
        Assert.That(seenContext?.CommandId, Is.SameAs(commandId));
        Assert.That(seenContext?.TraceId, Is.SameAs(traceId));
        Assert.That(seenContext?.Command, Is.SameAs(testCommand));
        Assert.That(seenContext?.ServiceProvider, Is.SameAs(scope.ServiceProvider));

        AssertLogEntryContains(LogLevel.Critical, "validation", testCommand.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPreExecutionHook_WhenHookReturnsFalse_PreExecutionMessageIsNotLogged()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.PreExecutionHook = _ => false);

        _ = await Handler.Handle(testCommand);

        AssertNoLogEntryContains(LogLevel.Information, "Executing command");
    }

    [Test]
    public async Task GivenConfiguredPostExecutionHook_HookIsCalledWithCorrectParameters()
    {
        var testCommand = new TestCommand(10);
        LoggingCommandPostExecutionContext? seenContext = null;

        var commandId = string.Empty;
        var traceId = "test-trace-id";

        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(traceId);

        using var scope = Host.Services.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        configurePipeline = b => b.UseLogging(o =>
        {
            commandId = b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetCommandId();

            o.PostExecutionLogLevel = LogLevel.Debug;

            o.PostExecutionHook = ctx =>
            {
                seenContext = ctx;
                return true;
            };
        });

        var response = await handler.Handle(testCommand);

        seenContext?.Logger.LogCritical("validation");

        Assert.That(seenContext, Is.Not.Null);
        Assert.That(seenContext?.LogLevel, Is.EqualTo(LogLevel.Debug));
        Assert.That(seenContext?.CommandId, Is.SameAs(commandId));
        Assert.That(seenContext?.TraceId, Is.SameAs(traceId));
        Assert.That(seenContext?.Command, Is.SameAs(testCommand));
        Assert.That(seenContext?.Response, Is.SameAs(response));
        Assert.That(seenContext?.ElapsedTime.Ticks, Is.GreaterThan(0));
        Assert.That(seenContext?.ServiceProvider, Is.SameAs(scope.ServiceProvider));

        AssertLogEntryContains(LogLevel.Critical, "validation", testCommand.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPostExecutionHook_WhenHookReturnsFalse_PostExecutionMessageIsNotLogged()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.PostExecutionHook = _ => false);

        _ = await Handler.Handle(testCommand);

        AssertNoLogEntryContains(LogLevel.Information, "Executed command");
    }

    [Test]
    public void GivenConfiguredExceptionHook_HookIsCalledWithCorrectParameters()
    {
        var testCommand = new TestCommand(10);
        var exception = new InvalidOperationException("test exception message");
        LoggingCommandExceptionContext? seenContext = null;

        var commandId = string.Empty;
        var traceId = "test-trace-id";

        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(traceId);

        using var scope = Host.Services.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        configurePipeline = b => b.UseLogging(o =>
        {
            commandId = b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetCommandId();

            o.ExceptionLogLevel = LogLevel.Critical;

            o.ExceptionHook = ctx =>
            {
                seenContext = ctx;
                return true;
            };
        });

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(testCommand));

        seenContext?.Logger.LogTrace("validation");

        Assert.That(seenContext, Is.Not.Null);
        Assert.That(seenContext?.LogLevel, Is.EqualTo(LogLevel.Critical));
        Assert.That(seenContext?.CommandId, Is.SameAs(commandId));
        Assert.That(seenContext?.TraceId, Is.SameAs(traceId));
        Assert.That(seenContext?.Command, Is.SameAs(testCommand));
        Assert.That(seenContext?.Exception, Is.SameAs(exception));
        Assert.That(seenContext?.ElapsedTime.Ticks, Is.GreaterThan(0));
        Assert.That(seenContext?.ServiceProvider, Is.SameAs(scope.ServiceProvider));

        AssertLogEntryContains(LogLevel.Trace, "validation", testCommand.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public void GivenConfiguredExceptionHook_WhenHookReturnsFalse_ExceptionMessageIsNotLogged()
    {
        var testCommand = new TestCommand(10);
        var exception = new InvalidOperationException("test exception message");

        configurePipeline = b => b.UseLogging(o => o.ExceptionHook = _ => false);

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.Handle(testCommand));

        AssertNoLogEntryContains(LogLevel.Error, "An exception occurred while executing command");
    }

    [Test]
    public async Task GivenConfiguredJsonSerializerSettings_CommandIsSerializedWithConfiguredSettingsPreExecution()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _ = await Handler.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"payload\":10}");
    }

    [Test]
    public async Task GivenConfiguredJsonSerializerSettings_ResponseIsSerializedWithConfiguredSettingsPostExecution()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _ = await Handler.Handle(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information, "{\"responsePayload\":10}");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevelWhichIsNotActive_CommandIsNotSerialized()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o =>
        {
            o.PreExecutionLogLevel = LogLevel.None;
            o.OmitJsonSerializedResponsePayload = true;
            o.JsonSerializerOptions = new() { PropertyNamingPolicy = new ThrowingJsonNamingPolicy() };
        });

        _ = await Handler.Handle(testCommand);

        AssertNoLogEntryContains(LogLevel.None, "Executing command");
    }

    [Test]
    public async Task GivenConfiguredPostExecutionLogLevelWhichIsNotActive_ResponseIsNotSerialized()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o =>
        {
            o.PostExecutionLogLevel = LogLevel.None;
            o.OmitJsonSerializedCommandPayload = true;
            o.JsonSerializerOptions = new() { PropertyNamingPolicy = new ThrowingJsonNamingPolicy() };
        });

        _ = await Handler.Handle(testCommand);

        AssertNoLogEntryContains(LogLevel.None, "Executed command");
    }

    [Test]
    public async Task GivenTraceId_LogsCorrectTraceId()
    {
        var testCommand = new TestCommand(10);

        var traceId = "test-trace-id";

        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetTraceId(traceId);

        _ = await Handler.Handle(testCommand);

        AssertLogEntryContains(LogLevel.Information, $"Trace ID: {traceId}", nrOfTimes: 2);
    }

    [Test]
    public async Task GivenCommandId_LogsCorrectCommandId()
    {
        var testCommand = new TestCommand(10);

        var commandId = string.Empty;

        configurePipeline = b =>
        {
            commandId = b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetCommandId();

            _ = b.UseLogging();
        };

        _ = await Handler.Handle(testCommand);

        AssertLogEntryContains(LogLevel.Information, $"Command ID: {commandId}", nrOfTimes: 2);
    }

    [Test]
    public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeConfigured()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging().ConfigureLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.Handle(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Debug, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeRemoved()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging().WithoutLogging();

        _ = await Handler.Handle(testCommand);

        AssertNoLogEntryContains(LogLevel.Debug, "Executing command");
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "middleware logic is to use lowercase")]
    private void AssertPreExecutionLogMessage(LogLevel logLevel,
                                              string? expectedSerializedCommand = null,
                                              string? expectedTransportTypeName = null,
                                              CommandTransportRole? expectedTransportRole = null,
                                              string? loggerName = null)
    {
        var transportTypeNameFragment = expectedTransportTypeName is null ? string.Empty : $" {expectedTransportTypeName}";
        var transportRoleFragment = expectedTransportRole is null ? string.Empty : $" on {Enum.GetName(expectedTransportRole.Value)?.ToLowerInvariant()}";

        if (expectedSerializedCommand is null)
        {
            var regexWithoutPayload = new Regex($@"Executing{transportTypeNameFragment} command{transportRoleFragment} \(Command ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
            AssertLogEntryMatches(logLevel, regexWithoutPayload, loggerName);
            return;
        }

        var regexWithPayload = new Regex($"Executing{transportTypeNameFragment} command{transportRoleFragment} with payload " + Regex.Escape(expectedSerializedCommand) + @" \(Command ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
        AssertLogEntryMatches(logLevel, regexWithPayload, loggerName);
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "middleware logic is to use lowercase")]
    private void AssertPostExecutionLogMessage(LogLevel logLevel,
                                               string? expectedSerializedResponse = null,
                                               string? expectedTransportTypeName = null,
                                               CommandTransportRole? expectedTransportRole = null,
                                               string? loggerName = null)
    {
        var transportTypeNameFragment = expectedTransportTypeName is null ? string.Empty : $" {expectedTransportTypeName}";
        var transportRoleFragment = expectedTransportRole is null ? string.Empty : $" on {Enum.GetName(expectedTransportRole.Value)?.ToLowerInvariant()}";

        if (expectedSerializedResponse is null)
        {
            var regexWithoutPayload = new Regex($@"Executed{transportTypeNameFragment} command{transportRoleFragment} in [0-9.]+ms \(Command ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
            AssertLogEntryMatches(logLevel, regexWithoutPayload, loggerName);
            return;
        }

        var regexWithPayload = new Regex($"Executed{transportTypeNameFragment} command{transportRoleFragment} and got response " + Regex.Escape(expectedSerializedResponse) + @" in [0-9.]+ms \(Command ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
        AssertLogEntryMatches(logLevel, regexWithPayload, loggerName);
    }

    private ICommandHandler<TestCommand, TestCommandResponse> Handler => Resolve<ICommandHandler<TestCommand, TestCommandResponse>>();

    private ICommandHandler<TestCommandWithoutResponse> HandlerWithoutResponse => Resolve<ICommandHandler<TestCommandWithoutResponse>>();

    private ICommandHandler<TestCommandWithoutPayload, TestCommandResponse> HandlerWithoutPayload => Resolve<ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(
                        async (command, _, _) =>
                        {
                            await Task.Yield();
                            return handlerFn(command);
                        },
                        pipeline => configurePipeline(pipeline))
                    .AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>(
                        async (_, _, _) => { await Task.Yield(); },
                        pipeline => configurePipelineWithoutResponse(pipeline))
                    .AddConquerorCommandHandlerDelegate<TestCommandWithoutPayload, TestCommandResponse>(
                        async (_, _, _) =>
                        {
                            await Task.Yield();
                            return new(0);
                        },
                        pipeline => configurePipelineWithoutPayload(pipeline))
                    .AddConquerorCommandClient<ICommandHandler<TestCommandWithTransport, TestCommandResponse>>(_ => new TestCommandTransportClient());
    }

    private sealed record TestCommand(int Payload);

    private sealed record TestCommandResponse(int ResponsePayload);

    private sealed record TestCommandWithoutResponse(int Payload);

    private sealed record TestCommandWithoutPayload;

    private sealed record TestCommandWithTransport;

    private sealed class ThrowingJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestCommandTransportClient : ICommandTransportClient
    {
        public string TransportTypeName => TestTransportTypeName;

        public async Task<TResponse> Send<TCommand, TResponse>(TCommand command, IServiceProvider serviceProvider, CancellationToken cancellationToken)
            where TCommand : class
        {
            serviceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.SignalExecutionFromTransport(TestTransportTypeName);
            var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var response = await handler.Handle(new(10), cancellationToken);
            return (TResponse)(object)new TestCommandResponse(response.ResponsePayload + 10);
        }
    }
}
