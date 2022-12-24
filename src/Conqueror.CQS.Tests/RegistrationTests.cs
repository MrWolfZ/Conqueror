using Conqueror.CQS.CommandHandling;
using Conqueror.CQS.QueryHandling;

namespace Conqueror.CQS.Tests
{
    [TestFixture]
    public sealed class RegistrationTests
    {
        [Test]
        public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
        {
            var services = new ServiceCollection().AddConquerorCQS().AddConquerorCQS();

            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(QueryServiceCollectionConfigurator)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(CommandServiceCollectionConfigurator)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromExecutingAssemblyAddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
        {
            var services1 = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);
            var services2 = new ServiceCollection().AddConquerorCQSTypesFromExecutingAssembly();

            Assert.AreEqual(services1.Count, services2.Count);
            Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsQueryHandlerWithPlainInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestQueryHandler) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsQueryHandlerWithCustomInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestQueryHandlerWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandHandlerWithPlainInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandHandler) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandHandlerWithCustomInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandHandlerWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandWithoutResponseHandlerWithPlainInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandWithoutResponseHandler) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandWithoutResponseHandlerWithCustomInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandWithoutResponseHandlerWithCustomInterface) &&
                                            d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsQueryMiddlewareAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestQueryMiddleware) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsQueryMiddlewareWithoutConfigurationAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestQueryMiddlewareWithoutConfiguration) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandMiddlewareAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandMiddleware) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandMiddlewareWithoutConfigurationAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandMiddlewareWithoutConfiguration) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollectionWithHandlerAlreadyRegistered_AddingAllTypesFromAssemblyDoesNotAddHandlerAgain()
        {
            var services = new ServiceCollection().AddSingleton<TestCommandHandler>().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.AreEqual(1, services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandHandler)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddInterfaces()
        {
            var services = new ServiceCollection().AddSingleton<TestCommandHandler>().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(ITestQueryHandler)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(ITestCommandHandler)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(ITestCommandWithoutResponseHandler)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddAbstractClasses()
        {
            var services = new ServiceCollection().AddSingleton<TestCommandHandler>().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestQueryHandler)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestCommandHandler)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestCommandHandlerWithCustomInterface)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestQueryMiddleware)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestCommandMiddleware)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddGenericClasses()
        {
            var services = new ServiceCollection().AddSingleton<TestCommandHandler>().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(GenericTestQueryHandler<>)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(GenericTestCommandHandler<>)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(GenericTestQueryMiddleware<>)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(GenericTestCommandMiddleware<>)));
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
        }

        private sealed class TestQueryHandlerWithCustomInterface : ITestQueryHandler
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
        }

        private abstract class AbstractTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
        }

        private sealed class GenericTestQueryHandler<T> : IQueryHandler<TestQuery, T>
            where T : new()
        {
            public Task<T> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new T());
        }

        private sealed class TestQueryMiddlewareConfiguration
        {
        }

        private sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        private sealed class TestQueryMiddlewareWithoutConfiguration : IQueryMiddleware
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        private abstract class AbstractTestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        private sealed class GenericTestQueryMiddleware<T> : IQueryMiddleware<T>
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, T> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        private sealed record TestCommand;

        private sealed record TestCommandResponse;

        private interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
        }

        private interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommand>
        {
        }

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
        }

        private sealed class TestCommandHandlerWithCustomInterface : ITestCommandHandler
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
        }

        private abstract class AbstractTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
        }

        private sealed class GenericTestCommandHandler<T> : ICommandHandler<TestCommand, T>
            where T : new()
        {
            public Task<T> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new T());
        }

        private abstract class AbstractTestCommandHandlerWithCustomInterface : ITestCommandHandler
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
        }

        private sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommand>
        {
            public Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class TestCommandWithoutResponseHandlerWithCustomInterface : ITestCommandWithoutResponseHandler
        {
            public Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class TestCommandMiddlewareConfiguration
        {
        }

        private sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }

        private sealed class TestCommandMiddlewareWithoutConfiguration : ICommandMiddleware
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }

        private abstract class AbstractTestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }

        private sealed class GenericTestCommandMiddleware<T> : ICommandMiddleware<T>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, T> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }
}
