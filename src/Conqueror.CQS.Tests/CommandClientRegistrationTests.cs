namespace Conqueror.CQS.Tests
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
    public sealed class CommandClientRegistrationTests
    {
        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClient()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ICommandHandler<TestCommand, TestCommandResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestCommandHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolvePlainClient()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ITestCommandHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestCommandHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClient()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ITestCommandHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
        }

        [Test]
        public void GivenRegisteredPlainClientWithoutResponse_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ICommandHandler<TestCommandWithoutResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenRegisteredPlainClientWithoutResponseWithAsyncClientFactory_CanResolvePlainClient()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ICommandHandler<TestCommandWithoutResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithoutResponse_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithoutResponseWithAsyncClientFactory_CanResolvePlainClient()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ITestCommandWithoutResponseHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithoutResponse_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandWithoutResponseHandler>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithoutResponseWithAsyncClientFactory_CanResolveCustomClient()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ITestCommandWithoutResponseHandler>();

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
        public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
                                                  {
                                                      await Task.CompletedTask;
                                                      return new TestCommandTransport();
                                                  })
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
        public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorCommandClient<ITestCommandHandler>(async _ =>
                                                  {
                                                      await Task.CompletedTask;
                                                      return new TestCommandTransport();
                                                  })
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
        public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredPlainClient_WhenRegisteringWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            }));
        }

        [Test]
        public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            }));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClient_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorCommandClient<ITestCommandHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClient_WhenRegisteringWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            }));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorCommandClient<ITestCommandHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            }));
        }

        [Test]
        public void GivenCustomInterfaceWithExtraMethods_WhenRegistering_ThrowsArgumentException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCommandClient<ITestCommandHandlerWithExtraMethod>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenCustomInterfaceWithExtraMethods_WhenRegisteringWithAsyncClientFactory_ThrowsArgumentException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCommandClient<ITestCommandHandlerWithExtraMethod>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            }));
        }

        [Test]
        public void GivenRegisteredAndFinalizedHandler_WhenRegisteringPlainClient_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenRegisteredAndFinalizedHandler_WhenRegisteringPlainClientWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            }));
        }

        [Test]
        public void GivenRegisteredAndFinalizedHandler_WhenRegisteringCustomClient_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport()));
        }

        [Test]
        public void GivenRegisteredAndFinalizedHandler_WhenRegisteringCustomClientWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            }));
        }

        [Test]
        public void GivenRegisteredHandlerAndPlainClient_WhenFinalizing_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenRegisteredHandlerAndPlainClientWithAsyncClientFactory_WhenFinalizing_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenRegisteredHandlerAndCustomClient_WhenFinalizing_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenRegisteredHandlerAndCustomClientWithAsyncClientFactory_WhenFinalizing_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestCommandHandler>().AddConquerorCommandClient<ITestCommandHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestCommandTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenClient_CanResolveConquerorContextAccessor()
        {
            using var provider = RegisterClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IConquerorContextAccessor>());
        }

        private static ServiceProvider RegisterClient<TCommandHandler>()
            where TCommandHandler : class, ICommandHandler
        {
            return new ServiceCollection().AddConquerorCQS()
                                          .AddConquerorCommandClient<TCommandHandler>(_ => new TestCommandTransport())
                                          .FinalizeConquerorRegistrations()
                                          .BuildServiceProvider();
        }

        private static ServiceProvider RegisterClientWithAsyncClientFactory<TCommandHandler>()
            where TCommandHandler : class, ICommandHandler
        {
            return new ServiceCollection().AddConquerorCQS()
                                          .AddConquerorCommandClient<TCommandHandler>(_ => Task.FromResult(new TestCommandTransport() as ICommandTransportClient))
                                          .FinalizeConquerorRegistrations()
                                          .BuildServiceProvider();
        }

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
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();

                throw new NotSupportedException("should never be called");
            }
        }
    }
}
