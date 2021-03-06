using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class QueryHandlerCustomInterfaceTests
    {
        [Test]
        public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromCustomInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
        }

        [Test]
        public async Task GivenHandlerWithCustomInterface_ResolvingHandlerViaPlainAndCustomInterfaceReturnsEquivalentInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var plainInterfaceHandler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var customInterfaceHandler = provider.GetRequiredService<ITestQueryHandler>();

            _ = await plainInterfaceHandler.ExecuteQuery(new(), CancellationToken.None);
            _ = await customInterfaceHandler.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreEqual(2, observations.Instances.Count);
            Assert.AreSame(observations.Instances[0], observations.Instances[1]);
        }

        [Test]
        public void GivenHandlerWithMultipleCustomInterfaces_HandlerCanBeResolvedFromAllInterfaces()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleInterfaces>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler2>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034

        public sealed record TestQuery;

        public sealed record TestQueryResponse;

        public sealed record TestQuery2;

        public sealed record TestQueryResponse2;

        public sealed record TestCommand;

        public sealed record TestCommandResponse;

        public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        public interface ITestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>
        {
        }

        public interface ITestQueryHandlerWithExtraMethod : IQueryHandler<TestQuery, TestQueryResponse>
        {
            void ExtraMethod();
        }

        private sealed class TestQueryHandler : ITestQueryHandler
        {
            private readonly TestObservations observations;

            public TestQueryHandler(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                return new();
            }
        }

        private sealed class TestQueryHandlerWithMultipleInterfaces : ITestQueryHandler,
                                                                      ITestQueryHandler2,
                                                                      ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestQueryHandlerWithMultipleInterfaces(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                return new();
            }

            public async Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                return new();
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                return new();
            }
        }

        private sealed class TestQueryHandlerWithCustomInterfaceWithExtraMethod : ITestQueryHandlerWithExtraMethod
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();

            public void ExtraMethod() => throw new NotSupportedException();
        }

        private sealed class TestObservations
        {
            public List<object> Instances { get; } = new();
        }
    }
}
