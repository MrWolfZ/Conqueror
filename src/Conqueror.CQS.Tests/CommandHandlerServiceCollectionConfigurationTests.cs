using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    [TestFixture]
    public sealed class CommandHandlerServiceCollectionConfigurationTests
    {
        [Test]
        public void GivenMultipleRegisteredIdenticalHandlerTypes_ConfiguringServiceCollectionDoesNotThrow()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandler>()
                                                  .AddTransient<TestCommandHandler>();

            Assert.DoesNotThrow(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenMultipleRegisteredHandlerTypesForSameCommandAndResponseTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandler>()
                                                  .AddTransient<DuplicateTestCommandHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }
        
        [Test]
        public void GivenMultipleRegisteredHandlerTypesForSameCommandAndDifferentResponseTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandler>()
                                                  .AddTransient<DuplicateTestCommandHandlerWithDifferentResponseType>();

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }
        
        [Test]
        public void GivenMultipleRegisteredHandlerTypesWithoutResponseForSameCommandType_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandWithoutResponseHandler>()
                                                  .AddTransient<DuplicateTestCommandWithoutResponseHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }

        private sealed record TestCommand;

        private sealed record TestCommandWithoutResponse;

        private sealed record TestCommandResponse;

        private sealed record TestCommandResponse2;

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestCommandHandlerWithDifferentResponseType : ICommandHandler<TestCommand, TestCommandResponse2>
        {
            public Task<TestCommandResponse2> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        private sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class DuplicateTestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
