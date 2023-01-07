﻿using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests
{
    [TestFixture]
    public sealed class CommandLoggingMiddlewareTests : TestBase
    {
        private Func<TestCommand, TestCommandResponse> handlerFn = cmd => new(cmd.Payload);
        private Action<ICommandPipelineBuilder> configurePipeline = b => b.UseLogging();

        [Test]
        public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsCommandWithPayloadPreExecution()
        {
            var testCommand = new TestCommand(10);

            _ = await Handler.ExecuteCommand(testCommand);

            AssertLogEntry(LogLevel.Information, $"Executing command with payload {{\"Payload\":{testCommand.Payload}}}");
        }

        [Test]
        public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsCommandThatHasNoPayloadPreExecution()
        {
            var testCommand = new TestCommandWithoutPayload();

            _ = await HandlerWithoutPayload.ExecuteCommand(testCommand);

            AssertLogEntry(LogLevel.Information, "Executing command");
        }

        [Test]
        public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsResponseWithPayloadPostExecution()
        {
            var testCommand = new TestCommand(10);

            var response = await Handler.ExecuteCommand(testCommand);

            AssertLogEntryContains(LogLevel.Information, $"Executed command and got response {{\"ResponsePayload\":{response.ResponsePayload}}} in");
        }

        [Test]
        public async Task GivenDefaultLoggingMiddlewareConfiguration_LogsForCommandWithoutResponsePostExecution()
        {
            var testCommand = new TestCommandWithoutResponse(10);

            await HandlerWithoutResponse.ExecuteCommand(testCommand);

            AssertLogEntryContains(LogLevel.Information, "Executed command in");
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
        }

        [Test]
        public async Task GivenConfiguredPreExecutionLogLevel_LogsCommandWithPayloadPreExecutionAtSpecifiedLevel()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

            _ = await Handler.ExecuteCommand(testCommand);

            AssertLogEntry(LogLevel.Debug, $"Executing command with payload {{\"Payload\":{testCommand.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredPreExecutionLogLevel_LogsCommandThatHasNoPayloadPreExecutionAtSpecifiedLevel()
        {
            var testCommand = new TestCommandWithoutPayload();

            configurePipeline = b => b.UseLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

            _ = await HandlerWithoutPayload.ExecuteCommand(testCommand);

            AssertLogEntry(LogLevel.Debug, "Executing command");
        }

        [Test]
        public async Task GivenConfiguredPostExecutionLogLevel_LogsResponseWithPayloadPostExecutionAtSpecifiedLevel()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging(o => o.PostExecutionLogLevel = LogLevel.Debug);

            var response = await Handler.ExecuteCommand(testCommand);

            AssertLogEntryContains(LogLevel.Debug, $"Executed command and got response {{\"ResponsePayload\":{response.ResponsePayload}}} in");
        }

        [Test]
        public async Task GivenConfiguredPostExecutionLogLevel_LogsForCommandWithoutResponsePostExecutionAtSpecifiedLevel()
        {
            var testCommand = new TestCommandWithoutResponse(10);

            configurePipeline = b => b.UseLogging(o => o.PostExecutionLogLevel = LogLevel.Debug);

            await HandlerWithoutResponse.ExecuteCommand(testCommand);

            AssertLogEntryContains(LogLevel.Debug, "Executed command in");
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
        }

        [Test]
        public async Task GivenConfiguredToOmitCommandPayload_LogsCommandWithoutPayloadPreExecution()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedCommandPayload = true);

            _ = await Handler.ExecuteCommand(testCommand);

            AssertLogEntry(LogLevel.Information, "Executing command");
        }

        [Test]
        public async Task GivenConfiguredToOmitCommandPayload_LogsCommandThatHasNoPayloadPreExecution()
        {
            var testCommand = new TestCommandWithoutPayload();

            configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedCommandPayload = true);

            _ = await HandlerWithoutPayload.ExecuteCommand(testCommand);

            AssertLogEntry(LogLevel.Information, "Executing command");
        }

        [Test]
        public async Task GivenConfiguredToOmitResponsePayload_LogsResponseWithoutPayloadPostExecution()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedResponsePayload = true);

            _ = await Handler.ExecuteCommand(testCommand);

            AssertLogEntryContains(LogLevel.Information, "Executed command in");
        }

        [Test]
        public async Task GivenConfiguredToOmitResponsePayload_LogsForCommandWithoutResponsePostExecution()
        {
            var testCommand = new TestCommandWithoutResponse(10);

            configurePipeline = b => b.UseLogging(o => o.OmitJsonSerializedResponsePayload = true);

            await HandlerWithoutResponse.ExecuteCommand(testCommand);

            AssertLogEntryContains(LogLevel.Information, "Executed command in");
        }

        [Test]
        public async Task GivenConfiguredLoggerNameFactory_LogsWithCorrectName()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = cmd => $"Custom{cmd.GetType().Name}");

            _ = await Handler.ExecuteCommand(testCommand);

            AssertLogEntry($"Custom{testCommand.GetType().Name}", LogLevel.Information, $"Executing command with payload {{\"Payload\":{testCommand.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredLoggerNameFactory_WhenFactoryReturnsNull_LogsWithLoggerWithDefaultName()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging(o => o.LoggerNameFactory = _ => null);

            _ = await Handler.ExecuteCommand(testCommand);

            AssertLogEntry(testCommand.GetType().FullName!.Replace("+", "."), LogLevel.Information, $"Executing command with payload {{\"Payload\":{testCommand.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredPreExecutionHook_HookIsCalledWithCorrectParameters()
        {
            var testCommand = new TestCommand(10);
            CommandLoggingPreExecutionContext? seenContext = null;

            using var scope = Host.Services.CreateScope();

            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            configurePipeline = b => b.UseLogging(o =>
            {
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
            Assert.AreEqual(LogLevel.Debug, seenContext?.LogLevel);
            Assert.AreSame(testCommand, seenContext?.Command);
            Assert.AreSame(scope.ServiceProvider, seenContext?.ServiceProvider);

            AssertLogEntry(testCommand.GetType().FullName!.Replace("+", "."), LogLevel.Critical, "validation");
        }

        [Test]
        public async Task GivenConfiguredPreExecutionHook_WhenHookReturnsFalse_PreExecutionMessageIsNotLogged()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging(o => o.PreExecutionHook = _ => false);

            _ = await Handler.ExecuteCommand(testCommand);

            AssertNoLogEntry(LogLevel.Information, $"Executing command with payload {{\"Payload\":{testCommand.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredPostExecutionHook_HookIsCalledWithCorrectParameters()
        {
            var testCommand = new TestCommand(10);
            CommandLoggingPostExecutionContext? seenContext = null;

            using var scope = Host.Services.CreateScope();

            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            configurePipeline = b => b.UseLogging(o =>
            {
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
            Assert.AreEqual(LogLevel.Debug, seenContext?.LogLevel);
            Assert.AreSame(testCommand, seenContext?.Command);
            Assert.AreSame(response, seenContext?.Response);
            Assert.IsTrue(seenContext?.ElapsedTime.Ticks > 0);
            Assert.AreSame(scope.ServiceProvider, seenContext?.ServiceProvider);

            AssertLogEntry(testCommand.GetType().FullName!.Replace("+", "."), LogLevel.Critical, "validation");
        }

        [Test]
        public async Task GivenConfiguredPostExecutionHook_WhenHookReturnsFalse_PostExecutionMessageIsNotLogged()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging(o => o.PostExecutionHook = _ => false);

            var response = await Handler.ExecuteCommand(testCommand);

            AssertNoLogEntryContains(LogLevel.Information, $"Executed command and got response {{\"ResponsePayload\":{response.ResponsePayload}}} in");
        }

        [Test]
        public void GivenConfiguredExceptionHook_HookIsCalledWithCorrectParameters()
        {
            var testCommand = new TestCommand(10);
            var exception = new InvalidOperationException("test exception message");
            CommandLoggingExceptionContext? seenContext = null;

            using var scope = Host.Services.CreateScope();

            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

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

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(testCommand));

            seenContext?.Logger.LogTrace("validation");

            Assert.That(seenContext, Is.Not.Null);
            Assert.AreEqual(LogLevel.Critical, seenContext?.LogLevel);
            Assert.AreSame(testCommand, seenContext?.Command);
            Assert.AreSame(exception, seenContext?.Exception);
            Assert.IsTrue(seenContext?.ElapsedTime.Ticks > 0);
            Assert.AreSame(scope.ServiceProvider, seenContext?.ServiceProvider);

            AssertLogEntry(testCommand.GetType().FullName!.Replace("+", "."), LogLevel.Trace, "validation");
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

            AssertLogEntry(LogLevel.Information, $"Executing command with payload {{\"payload\":{testCommand.Payload}}}");
        }

        [Test]
        public async Task GivenConfiguredJsonSerializerSettings_ResponseIsSerializedWithConfiguredSettingsPostExecution()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging(o => o.JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var response = await Handler.ExecuteCommand(testCommand);

            AssertLogEntryContains(LogLevel.Information, $"Executed command and got response {{\"responsePayload\":{response.ResponsePayload}}} in");
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
        public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeConfigured()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging().ConfigureLogging(o => o.PreExecutionLogLevel = LogLevel.Debug);

            _ = await Handler.ExecuteCommand(testCommand);

            AssertLogEntry(LogLevel.Debug, $"Executing command with payload {{\"Payload\":{testCommand.Payload}}}");
        }

        [Test]
        public async Task GivenPipelineWithLoggingMiddleware_MiddlewareCanBeRemoved()
        {
            var testCommand = new TestCommand(10);

            configurePipeline = b => b.UseLogging().WithoutLogging();

            _ = await Handler.ExecuteCommand(testCommand);

            AssertNoLogEntry(LogLevel.Debug, $"Executing command with payload {{\"Payload\":{testCommand.Payload}}}");
        }

        private ICommandHandler<TestCommand, TestCommandResponse> Handler => Resolve<ICommandHandler<TestCommand, TestCommandResponse>>();

        private ICommandHandler<TestCommandWithoutResponse> HandlerWithoutResponse => Resolve<ICommandHandler<TestCommandWithoutResponse>>();

        private ICommandHandler<TestCommandWithoutPayload, TestCommandResponse> HandlerWithoutPayload => Resolve<ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>>();

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddConquerorCQS()
                        .AddConquerorCQSLoggingMiddlewares()
                        .AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandWithoutResponseHandler>()
                        .AddTransient<TestCommandWithoutPayloadHandler>()
                        .AddTransient<Func<TestCommand, TestCommandResponse>>(_ => cmd => handlerFn.Invoke(cmd))
                        .AddTransient<Action<ICommandPipelineBuilder>>(_ => b => configurePipeline.Invoke(b))
                        .FinalizeConquerorRegistrations();
        }

        private sealed record TestCommand(int Payload);

        private sealed record TestCommandResponse(int ResponsePayload);

        private sealed record TestCommandWithoutResponse(int Payload);

        private sealed record TestCommandWithoutPayload;

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
        {
            private readonly Func<TestCommand, TestCommandResponse> handlerFn;

            public TestCommandHandler(Func<TestCommand, TestCommandResponse> handlerFn)
            {
                this.handlerFn = handlerFn;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return handlerFn(command);
            }

            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                var configure = pipeline.ServiceProvider.GetRequiredService<Action<ICommandPipelineBuilder>>();
                configure(pipeline);
            }
        }

        private sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>, IConfigureCommandPipeline
        {
            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
            }

            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                var configure = pipeline.ServiceProvider.GetRequiredService<Action<ICommandPipelineBuilder>>();
                configure(pipeline);
            }
        }

        private sealed class TestCommandWithoutPayloadHandler : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>, IConfigureCommandPipeline
        {
            public async Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return new(0);
            }

            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                var configure = pipeline.ServiceProvider.GetRequiredService<Action<ICommandPipelineBuilder>>();
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
