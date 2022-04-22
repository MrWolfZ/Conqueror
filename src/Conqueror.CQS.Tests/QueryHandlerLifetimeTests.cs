using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class QueryHandlerLifetimeTests
    {
        [Test]
        public async Task GivenTransientHandler_ResolvingHandlerCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedHandler_ResolvingHandlerCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddScoped<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
        }

        [Test]
        public async Task GivenSingletonHandler_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        private sealed record TestCommand;

        private sealed record TestCommandResponse;

        private sealed class TestQueryHandler : IQueryHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryHandler(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestCommandResponse> ExecuteQuery(TestCommand command, CancellationToken cancellationToken)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                return new();
            }
        }

        private sealed class TestObservations
        {
            public List<int> InvocationCounts { get; } = new();
        }
    }
}
