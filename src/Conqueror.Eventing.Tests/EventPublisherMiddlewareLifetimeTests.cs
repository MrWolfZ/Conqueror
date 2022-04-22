using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.Eventing.Tests
{
    public sealed class EventPublisherMiddlewareLifetimeTests
    {
        [Test]
        public async Task GivenTransientMiddleware_ResolvingObserverCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);
            await observer3.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedMiddleware_ResolvingObserverCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddScoped<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);
            await observer3.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
        }

        [Test]
        public async Task GivenSingletonMiddleware_ResolvingObserverReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddSingleton<TestEventPublisherMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);
            await observer3.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task GivenMultipleTransientMiddlewares_ResolvingObserverCreatesNewInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);
            await observer3.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenMultipleScopedMiddlewares_ResolvingObserverCreatesNewInstancesForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddScoped<TestEventPublisherMiddleware>()
                        .AddScoped<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);
            await observer3.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 1, 1 }));
        }

        [Test]
        public async Task GivenMultipleSingletonMiddlewares_ResolvingObserverReturnsSameInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddSingleton<TestEventPublisherMiddleware>()
                        .AddSingleton<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);
            await observer3.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3 }));
        }

        [Test]
        public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingObserverReturnsInstancesAccordingToEachLifetime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddSingleton<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);
            await observer3.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3 }));
        }

        [Test]
        public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionGetsNewInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventPublisherRetryMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventPublisherRetryMiddleware>()
                        .AddScoped<TestEventPublisherMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 1, 1, 2, 1 }));
        }

        [Test]
        public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorEventing()
                        .AddTransient<TestEventObserver>()
                        .AddTransient<TestEventPublisherRetryMiddleware>()
                        .AddSingleton<TestEventPublisherMiddleware>()
                        .AddTransient<TestEventPublisherMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
            var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

            await observer1.HandleEvent(new(), CancellationToken.None);
            await observer2.HandleEvent(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
        }

        private sealed record TestEvent;

        private sealed class TestEventObserver : IEventObserver<TestEvent>
        {
            public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
            {
                await Task.Yield();
            }
        }

        private sealed class TestEventPublisherMiddleware : IEventPublisherMiddleware
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestEventPublisherMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
                where TEvent : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class TestEventPublisherMiddleware2 : IEventPublisherMiddleware
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestEventPublisherMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
                where TEvent : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class TestEventPublisherRetryMiddleware : IEventPublisherMiddleware
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestEventPublisherRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
                where TEvent : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                await ctx.Next(ctx.Event, ctx.CancellationToken);
                await ctx.Next(ctx.Event, ctx.CancellationToken);
            }
        }

        private sealed class TestObservations
        {
            public List<int> InvocationCounts { get; } = new();
        }
    }
}
