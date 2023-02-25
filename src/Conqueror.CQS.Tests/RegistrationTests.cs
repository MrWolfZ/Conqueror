using Conqueror.Common;
using Conqueror.CQS.CommandHandling;
using Conqueror.CQS.QueryHandling;

namespace Conqueror.CQS.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for assembly scanning to work")]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "for testing purposes we want to mix public and private classes")]
    public sealed class RegistrationTests
    {
        [Test]
        public void GivenServiceCollectionWithMultipleRegisteredHandlers_DoesNotRegisterConquerorTypesMultipleTimes()
        {
            var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                                  .AddConquerorCommandHandler<TestCommand2Handler>()
                                                  .AddConquerorQueryHandler<TestQueryHandler>()
                                                  .AddConquerorQueryHandler<TestQuery2Handler>();

            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(CommandClientFactory)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICommandClientFactory)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(CommandHandlerRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICommandHandlerRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(CommandMiddlewareRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICommandMiddlewareRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(CommandContextAccessor)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICommandContextAccessor)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(QueryClientFactory)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IQueryClientFactory)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(QueryHandlerRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IQueryHandlerRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(QueryMiddlewareRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IQueryMiddlewareRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(QueryContextAccessor)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IQueryContextAccessor)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ConquerorContextAccessor)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IConquerorContextAccessor)));
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
            var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>(ServiceLifetime.Singleton)
                                                  .AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.AreEqual(1, services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandHandler)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsInterfaces()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResponse>)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ITestQueryHandler)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICommandHandler<TestCommand, TestCommandResponse>)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ITestCommandHandler)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICommandHandler<TestCommandWithoutResponse>)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ITestCommandWithoutResponseHandler)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddAbstractClasses()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestQueryHandler)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestCommandHandler)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestCommandHandlerWithCustomInterface)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestQueryMiddleware)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestCommandMiddleware)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddGenericClasses()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(GenericTestQueryHandler<>)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(GenericTestCommandHandler<>)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(GenericTestQueryMiddleware<>)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(GenericTestCommandMiddleware<>)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddPrivateClasses()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(PrivateTestQueryHandler)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(PrivateTestCommandHandler)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(PrivateTestQueryMiddleware)));
            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(PrivateTestCommandMiddleware)));
        }

        public sealed record TestQuery;

        public sealed record TestQueryResponse;

        public sealed record TestQuery2;

        public sealed record TestQuery2Response;

        public sealed record TestQueryWithCustomInterface;

        public interface ITestQueryHandler : IQueryHandler<TestQueryWithCustomInterface, TestQueryResponse>
        {
        }

        public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
        }

        public sealed class TestQueryHandlerWithCustomInterface : ITestQueryHandler
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQueryWithCustomInterface query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
        }

        public sealed class TestQuery2Handler : IQueryHandler<TestQuery2, TestQuery2Response>
        {
            public Task<TestQuery2Response> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQuery2Response());
        }

        public abstract class AbstractTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
        }

        public sealed class GenericTestQueryHandler<T> : IQueryHandler<TestQuery, T>
            where T : new()
        {
            public Task<T> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new T());
        }

        private sealed class PrivateTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
        }

        public sealed class TestQueryMiddlewareConfiguration
        {
        }

        public sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        public sealed class TestQueryMiddlewareWithoutConfiguration : IQueryMiddleware
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        public abstract class AbstractTestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        public sealed class GenericTestQueryMiddleware<T> : IQueryMiddleware<T>
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, T> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        private sealed class PrivateTestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        public sealed record TestCommand;

        public sealed record TestCommandResponse;

        public sealed record TestCommand2;

        public sealed record TestCommand2Response;

        public sealed record TestCommandWithoutResponse;

        public sealed record TestCommandWithCustomInterface;

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

        public abstract class AbstractTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
        }

        public sealed class GenericTestCommandHandler<T> : ICommandHandler<TestCommand, T>
            where T : new()
        {
            public Task<T> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new T());
        }

        public abstract class AbstractTestCommandHandlerWithCustomInterface : ITestCommandHandler
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommandWithCustomInterface command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
        }

        public sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        public sealed class TestCommandWithoutResponseHandlerWithCustomInterface : ITestCommandWithoutResponseHandler
        {
            public Task ExecuteCommand(TestCommandWithoutResponseWithCustomInterface command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class PrivateTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
        }

        public sealed class TestCommandMiddlewareConfiguration
        {
        }

        public sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }

        public sealed class TestCommandMiddlewareWithoutConfiguration : ICommandMiddleware
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }

        public abstract class AbstractTestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }

        public sealed class GenericTestCommandMiddleware<T> : ICommandMiddleware<T>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, T> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }

        private sealed class PrivateTestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }
}
