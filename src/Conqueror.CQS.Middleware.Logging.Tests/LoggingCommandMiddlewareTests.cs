using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests;

[TestFixture]
public sealed class LoggingCommandMiddlewareTests : TestBase
{
    private Func<TestCommand, TestCommandResponse> handlerFn = cmd => new(cmd.Payload);
    private Action<ICommandPipelineBuilder> configurePipeline = b => b.UseLogging();

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsCommandWithPayloadPreExecution()
    {
        var testCommand = new TestCommand(10);

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsCommandThatHasNoPayloadPreExecution()
    {
        var testCommand = new TestCommandWithoutPayload();

        _ = await HandlerWithoutPayload.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsResponseWithPayloadPostExecution()
    {
        var testCommand = new TestCommand(10);

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information, "{\"ResponsePayload\":10}");
    }

    [Test]
    public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsForCommandWithoutResponsePostExecution()
    {
        var testCommand = new TestCommandWithoutResponse(10);

        await HandlerWithoutResponse.ExecuteCommand(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public void GivenDefaultLoggingMiddlewareConfiguration_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTrace()
    {
        var testCommand = new TestCommand(10);
        var exception = new InvalidOperationException("test exception message");

        handlerFn = _ => throw exception;

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteCommand(testCommand));

        AssertLogEntryContains(LogLevel.Error, "An exception occurred while executing command");
        AssertLogEntryContains(LogLevel.Error, exception.Message);
        AssertLogEntryContains(LogLevel.Error, exception.StackTrace![..exception.StackTrace!.IndexOf("---", StringComparison.Ordinal)]);
        AssertLogEntryContains(LogLevel.Error, "Command ID: ");
        AssertLogEntryContains(LogLevel.Error, "Trace ID: ");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevel_LogsCommandWithPayloadPreExecutionAtSpecifiedLevel()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Debug, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenConfiguredPreExecutionLogLevel_LogsCommandThatHasNoPayloadPreExecutionAtSpecifiedLevel()
    {
        var testCommand = new TestCommandWithoutPayload();

        configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await HandlerWithoutPayload.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Debug);
    }

    [Test]
    public async Task GivenConfiguredPostExecutionLogLevel_LogsResponseWithPayloadPostExecutionAtSpecifiedLevel()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.PostExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Debug, "{\"ResponsePayload\":10}");
    }

    [Test]
    public async Task GivenConfiguredPostExecutionLogLevel_LogsForCommandWithoutResponsePostExecutionAtSpecifiedLevel()
    {
        var testCommand = new TestCommandWithoutResponse(10);

        configurePipeline = b => b.UseLogging(o => o.PostExecutionLogLevel = LogLevel.Debug);

        await HandlerWithoutResponse.ExecuteCommand(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Debug);
    }

    [Test]
    public void GivenConfiguredExceptionLogLevel_WhenHandlerThrowsException_LogsExceptionWithMessageAndStackTraceAtSpecifiedLevel()
    {
        var testCommand = new TestCommand(10);
        var exception = new InvalidOperationException("test exception message");

        handlerFn = _ => throw exception;

        configurePipeline = b => b.UseLogging(o => o.ExceptionLogLevel = LogLevel.Critical);

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteCommand(testCommand));

        AssertLogEntryContains(LogLevel.Critical, "An exception occurred while executing command");
        AssertLogEntryContains(LogLevel.Critical, exception.Message);
        AssertLogEntryContains(LogLevel.Critical, exception.StackTrace![..exception.StackTrace!.IndexOf("---", StringComparison.Ordinal)]);
        AssertLogEntryContains(LogLevel.Critical, "Command ID: ");
        AssertLogEntryContains(LogLevel.Critical, "Trace ID: ");
    }

    [Test]
    public async Task GivenConfiguredToOmitCommandPayload_LogsCommandWithoutPayloadPreExecution()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedCommandPayload = true);

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitCommandPayload_LogsCommandThatHasNoPayloadPreExecution()
    {
        var testCommand = new TestCommandWithoutPayload();

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedCommandPayload = true);

        _ = await HandlerWithoutPayload.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitResponsePayload_LogsResponseWithoutPayloadPostExecution()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedResponsePayload = true);

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredToOmitResponsePayload_LogsForCommandWithoutResponsePostExecution()
    {
        var testCommand = new TestCommandWithoutResponse(10);

        configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedResponsePayload = true);

        await HandlerWithoutResponse.ExecuteCommand(testCommand);

        AssertPostExecutionLogMessage(LogLevel.Information);
    }

    [Test]
    public async Task GivenConfiguredLoggerNameFactory_LogsWithCorrectName()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = cmd => $"Custom{cmd.GetType().Name}");

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", $"Custom{testCommand.GetType().Name}");
    }

    [Test]
    public async Task GivenConfiguredLoggerNameFactory_WhenFactoryReturnsNull_LogsWithLoggerWithDefaultName()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = _ => null);

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"Payload\":10}", testCommand.GetType().FullName!.Replace("+", "."));
    }

    [Test]
    public async Task GivenConfiguredPreExecutionHook_HookIsCalledWithCorrectParameters()
    {
        var testCommand = new TestCommand(10);
        LoggingCommandPreExecutionContext? seenContext = null;

        var commandId = string.Empty;
        var traceId = "test-trace-id";

        Resolve<IConquerorContextAccessor>().GetOrCreate().SetTraceId(traceId);

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

        _ = await handler.ExecuteCommand(testCommand);

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

        _ = await Handler.ExecuteCommand(testCommand);

        AssertNoLogEntryContains(LogLevel.Information, "Executing command");
    }

    [Test]
    public async Task GivenConfiguredPostExecutionHook_HookIsCalledWithCorrectParameters()
    {
        var testCommand = new TestCommand(10);
        LoggingCommandPostExecutionContext? seenContext = null;

        var commandId = string.Empty;
        var traceId = "test-trace-id";

        Resolve<IConquerorContextAccessor>().GetOrCreate().SetTraceId(traceId);

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

        var response = await handler.ExecuteCommand(testCommand);

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

        _ = await Handler.ExecuteCommand(testCommand);

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

        Resolve<IConquerorContextAccessor>().GetOrCreate().SetTraceId(traceId);

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

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(testCommand));

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

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteCommand(testCommand));

        AssertNoLogEntryContains(LogLevel.Error, "An exception occurred while executing command");
    }

    [Test]
    public async Task GivenConfiguredJsonSerializerSettings_CommandIsSerializedWithConfiguredSettingsPreExecution()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Information, "{\"payload\":10}");
    }

    [Test]
    public async Task GivenConfiguredJsonSerializerSettings_ResponseIsSerializedWithConfiguredSettingsPostExecution()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _ = await Handler.ExecuteCommand(testCommand);

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

        _ = await Handler.ExecuteCommand(testCommand);

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

        _ = await Handler.ExecuteCommand(testCommand);

        AssertNoLogEntryContains(LogLevel.None, "Executed command");
    }

    [Test]
    public async Task GivenTraceId_LogsCorrectTraceId()
    {
        var testCommand = new TestCommand(10);

        var traceId = "test-trace-id";

        Resolve<IConquerorContextAccessor>().GetOrCreate().SetTraceId(traceId);

        _ = await Handler.ExecuteCommand(testCommand);

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

        _ = await Handler.ExecuteCommand(testCommand);

        AssertLogEntryContains(LogLevel.Information, $"Command ID: {commandId}", nrOfTimes: 2);
    }

    [Test]
    public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeConfigured()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging().ConfigureLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

        _ = await Handler.ExecuteCommand(testCommand);

        AssertPreExecutionLogMessage(LogLevel.Debug, "{\"Payload\":10}");
    }

    [Test]
    public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeRemoved()
    {
        var testCommand = new TestCommand(10);

        configurePipeline = b => b.UseLogging().WithoutLogging();

        _ = await Handler.ExecuteCommand(testCommand);

        AssertNoLogEntryContains(LogLevel.Debug, "Executing command");
    }

    private void AssertPreExecutionLogMessage(LogLevel logLevel, string? expectedSerializedCommand = null, string? loggerName = null)
    {
        if (expectedSerializedCommand is null)
        {
            var regexWithoutPayload = new Regex(@"Executing command \(Command ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
            AssertLogEntryMatches(logLevel, regexWithoutPayload, loggerName);
            return;
        }

        var regexWithPayload = new Regex(@"Executing command with payload " + Regex.Escape(expectedSerializedCommand) + @" \(Command ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
        AssertLogEntryMatches(logLevel, regexWithPayload, loggerName);
    }

    private void AssertPostExecutionLogMessage(LogLevel logLevel, string? expectedSerializedResponse = null, string? loggerName = null)
    {
        if (expectedSerializedResponse is null)
        {
            var regexWithoutPayload = new Regex(@"Executed command in [0-9.]+ms \(Command ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
            AssertLogEntryMatches(logLevel, regexWithoutPayload, loggerName);
            return;
        }

        var regexWithPayload = new Regex(@"Executed command and got response " + Regex.Escape(expectedSerializedResponse) + @" in [0-9.]+ms \(Command ID: [a-z0-9]+, Trace ID: [a-z0-9]+\)");
        AssertLogEntryMatches(logLevel, regexWithPayload, loggerName);
    }

    private ICommandHandler<TestCommand, TestCommandResponse> Handler => Resolve<ICommandHandler<TestCommand, TestCommandResponse>>();

    private ICommandHandler<TestCommandWithoutResponse> HandlerWithoutResponse => Resolve<ICommandHandler<TestCommandWithoutResponse>>();

    private ICommandHandler<TestCommandWithoutPayload, TestCommandResponse> HandlerWithoutPayload => Resolve<ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSLoggingMiddlewares()
                    .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(
                        async (command, _, _) =>
                        {
                            await Task.Yield();
                            return handlerFn(command);
                        },
                        pipeline => configurePipeline(pipeline))
                    .AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>(
                        async (_, _, _) => { await Task.Yield(); },
                        pipeline => configurePipeline(pipeline))
                    .AddConquerorCommandHandlerDelegate<TestCommandWithoutPayload, TestCommandResponse>(
                        async (_, _, _) =>
                        {
                            await Task.Yield();
                            return new(0);
                        },
                        pipeline => configurePipeline(pipeline));
    }

    private sealed record TestCommand(int Payload);

    private sealed record TestCommandResponse(int ResponsePayload);

    private sealed record TestCommandWithoutResponse(int Payload);

    private sealed record TestCommandWithoutPayload;

    private sealed class ThrowingJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            throw new NotSupportedException();
        }
    }
}
