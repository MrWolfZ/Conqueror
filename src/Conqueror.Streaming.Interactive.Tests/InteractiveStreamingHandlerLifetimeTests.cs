using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Interactive.Tests
{
    public sealed class InteractiveStreamingHandlerLifetimeTests
    {
        [Test]
        public async Task GivenTransientHandler_ResolvingHandlerCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddTransient<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();

            _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedHandler_ResolvingHandlerCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddScoped<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();

            _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
        }

        [Test]
        public async Task GivenSingletonHandler_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddSingleton<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();

            _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task GivenSingletonHandlerWithMultipleHandlerInterfaces_ResolvingHandlerViaEitherInterfaceReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddSingleton<TestStreamingHandlerWithMultipleInterfaces>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler1 = provider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();
            var handler2 = provider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming2, TestItem2>>();

            _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
        }

        [Test]
        public async Task GivenSingletonHandler_ResolvingHandlerViaConcreteClassReturnsSameInstanceAsResolvingViaInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddSingleton<TestStreamingHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler1 = provider.GetRequiredService<TestStreamingHandler>();
            var handler2 = provider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();

            _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
        }

        [Test]
        public async Task GivenSingletonHandlerInstance_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorInteractiveStreaming()
                        .AddSingleton(new TestStreamingHandler(observations));

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IInteractiveStreamingHandler<TestStreaming, TestItem>>();

            _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
            _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        private sealed record TestStreaming;

        private sealed record TestItem;

        private sealed record TestStreaming2;

        private sealed record TestItem2;

        private sealed class TestStreamingHandler : IInteractiveStreamingHandler<TestStreaming, TestItem>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestStreamingHandler(TestObservations observations)
            {
                this.observations = observations;
            }

            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreaming command, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                yield break;
            }
        }

        private sealed class TestStreamingHandlerWithMultipleInterfaces : IInteractiveStreamingHandler<TestStreaming, TestItem>,
                                                                          IInteractiveStreamingHandler<TestStreaming2, TestItem2>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestStreamingHandlerWithMultipleInterfaces(TestObservations observations)
            {
                this.observations = observations;
            }

            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreaming command, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                yield break;
            }

            public async IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreaming2 command, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                yield break;
            }
        }

        private sealed class TestObservations
        {
            public List<int> InvocationCounts { get; } = new();
        }
    }
}
