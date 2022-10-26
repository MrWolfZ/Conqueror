namespace Conqueror.CQS.Tests
{
    public sealed class CommandClientRegistrationTests
    {
        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestCommandHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestCommandHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
        }

        [Test]
        public void GivenRegisteredPlainClientWithoutResponse_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ICommandHandler<TestCommandWithoutResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithoutResponse_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithoutResponse_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandWithoutResponseHandler>());
        }

        [Test]
        public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestCommandHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICommandHandler<UnregisteredTestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestCommandHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IUnregisteredTestCommandHandler>());
        }

        [Test]
        public void GivenUnregisteredPlainClientWithoutResponse_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICommandHandler<UnregisteredTestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenUnregisteredCustomClientWithoutResponse_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IUnregisteredTestCommandWithoutResponseHandler>());
        }

        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport())
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport())
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
        }

        [Test]
        public void GivenAlreadyRegisteredPlainClient_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClient_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenCustomInterfaceWithExtraMethods_WhenRegistering_ThrowsArgumentException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCommandClient<ITestCommandHandlerWithExtraMethod>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenRegisteredAndConfiguredHandler_WhenRegisteringPlainClient_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().ConfigureConqueror();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenRegisteredAndConfiguredHandler_WhenRegisteringCustomClient_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().ConfigureConqueror();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenRegisteredHandlerAndPlainClient_WhenConfiguring_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenRegisteredHandlerAndCustomClient_WhenConfiguring_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenClient_CanResolveConquerorContextAccessor()
        {
            using var provider = RegisterClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IConquerorContextAccessor>());
        }

        private ServiceProvider RegisterClient<TCommandHandler>()
            where TCommandHandler : class, ICommandHandler
        {
            return new ServiceCollection().AddConquerorCQS()
                                          .AddConquerorCommandClient<TCommandHandler>(_ => new TestCommandTransport())
                                          .ConfigureConqueror()
                                          .BuildServiceProvider();
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034

        public sealed record TestCommand;

        public sealed record TestCommandResponse;

        public sealed record TestCommandWithoutResponse;

        public sealed record UnregisteredTestCommand;

        public sealed record UnregisteredTestCommandWithoutResponse;

        public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
        }

        public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
        }

        public interface ITestCommandHandlerWithExtraMethod : ICommandHandler<TestCommand, TestCommandResponse>
        {
            void ExtraMethod();
        }

        public interface IUnregisteredTestCommandHandler : ICommandHandler<UnregisteredTestCommand, TestCommandResponse>
        {
        }

        public interface IUnregisteredTestCommandWithoutResponseHandler : ICommandHandler<UnregisteredTestCommandWithoutResponse>
        {
        }

        private sealed class TestCommandTransport : ICommandTransportClient
        {
            public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
                where TCommand : class
            {
                await Task.Yield();

                throw new NotSupportedException("should never be called");
            }
        }

        private sealed class TestCommandHandler : ITestCommandHandler
        {
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();

                throw new NotSupportedException("should never be called");
            }
        }
    }
}
