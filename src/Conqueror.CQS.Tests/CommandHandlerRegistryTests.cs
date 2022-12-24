namespace Conqueror.CQS.Tests
{
    [TestFixture]
    public sealed class CommandHandlerRegistryTests
    {
        [Test]
        public void GivenManuallyRegisteredCommandHandler_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandler>()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

            var expectedRegistrations = new[]
            {
                new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandler)),
            };

            var registrations = registry.GetCommandHandlerRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenManuallyRegisteredCommandHandlerWithCustomInterface_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandlerWithCustomInterface>()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

            var expectedRegistrations = new[]
            {
                new CommandHandlerRegistration(typeof(TestCommandWithCustomInterface), typeof(TestCommandResponse), typeof(TestCommandHandlerWithCustomInterface)),
            };

            var registrations = registry.GetCommandHandlerRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenManuallyRegisteredCommandHandlerWithoutResponse_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandWithoutResponseHandler>()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

            var expectedRegistrations = new[]
            {
                new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(TestCommandWithoutResponseHandler)),
            };

            var registrations = registry.GetCommandHandlerRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenManuallyRegisteredCommandHandlerWithCustomInterfaceWithoutResponse_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandWithoutResponseHandlerWithCustomInterface>()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

            var expectedRegistrations = new[]
            {
                new CommandHandlerRegistration(typeof(TestCommandWithoutResponseWithCustomInterface), null, typeof(TestCommandWithoutResponseHandlerWithCustomInterface)),
            };

            var registrations = registry.GetCommandHandlerRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenMultipleManuallyRegisteredCommandHandlers_ReturnsRegistrations()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandHandler>()
                                                  .AddTransient<TestCommand2Handler>()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

            var expectedRegistrations = new[]
            {
                new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandler)),
                new CommandHandlerRegistration(typeof(TestCommand2), typeof(TestCommand2Response), typeof(TestCommand2Handler)),
            };

            var registrations = registry.GetCommandHandlerRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenCommandHandlersRegisteredViaAssemblyScanning_ReturnsRegistrations()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddConquerorCQSTypesFromExecutingAssembly()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

            var registrations = registry.GetCommandHandlerRegistrations();

            Assert.That(registrations, Contains.Item(new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandler))));
            Assert.That(registrations, Contains.Item(new CommandHandlerRegistration(typeof(TestCommand2), typeof(TestCommand2Response), typeof(TestCommand2Handler))));
        }

// types must be public for dynamic type generation and assembly scanning to work
#pragma warning disable CA1034

        public sealed record TestCommand;

        public sealed record TestCommandResponse;

        public sealed record TestCommandWithCustomInterface;

        public sealed record TestCommand2;

        public sealed record TestCommand2Response;

        public sealed record TestCommandWithoutResponse;

        public sealed record TestCommandWithoutResponseWithCustomInterface;

        public interface ITestCommandHandler : ICommandHandler<TestCommandWithCustomInterface, TestCommandResponse>
        {
        }

        public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponseWithCustomInterface>
        {
        }

        public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
        }

        public sealed class TestCommandHandlerWithCustomInterface : ITestCommandHandler
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommandWithCustomInterface command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
        }

        public sealed class TestCommand2Handler : ICommandHandler<TestCommand2, TestCommand2Response>
        {
            public Task<TestCommand2Response> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommand2Response());
        }

        public sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        public sealed class TestCommandWithoutResponseHandlerWithCustomInterface : ITestCommandWithoutResponseHandler
        {
            public Task ExecuteCommand(TestCommandWithoutResponseWithCustomInterface command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
