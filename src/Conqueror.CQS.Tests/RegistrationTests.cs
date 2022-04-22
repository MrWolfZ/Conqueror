using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.CommandHandling;
using Conqueror.CQS.QueryHandling;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    [TestFixture]
    public sealed class RegistrationTests
    {
        [Test]
        public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
        {
            var services = new ServiceCollection().AddConquerorCQS().AddConquerorCQS();

            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(QueryHandlerRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(QueryMiddlewaresInvoker)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(QueryServiceCollectionConfigurator)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(CommandHandlerRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(CommandMiddlewaresInvoker)));
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
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandMiddlewareAsTransient()
        {
            var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandMiddleware) && d.Lifetime == ServiceLifetime.Transient));
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
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken) => Task.FromResult(new TestQueryResponse());
        }

        private sealed class TestQueryHandlerWithCustomInterface : ITestQueryHandler
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken) => Task.FromResult(new TestQueryResponse());
        }

        private abstract class AbstractTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken) => Task.FromResult(new TestQueryResponse());
        }

        private sealed class TestQueryMiddlewareAttribute : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<TestQueryMiddleware>
        {
        }

        private sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareAttribute>
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareAttribute> ctx)
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
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => Task.FromResult(new TestCommandResponse());
        }

        private sealed class TestCommandHandlerWithCustomInterface : ITestCommandHandler
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => Task.FromResult(new TestCommandResponse());
        }

        private abstract class AbstractTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => Task.FromResult(new TestCommandResponse());
        }

        private abstract class AbstractTestCommandHandlerWithCustomInterface : ITestCommandHandler
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => Task.FromResult(new TestCommandResponse());
        }

        private sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommand>
        {
            public Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class TestCommandWithoutResponseHandlerWithCustomInterface : ITestCommandWithoutResponseHandler
        {
            public Task ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class TestCommandMiddlewareAttribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<TestCommandMiddleware>
        {
        }

        private sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareAttribute>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareAttribute> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }
}
