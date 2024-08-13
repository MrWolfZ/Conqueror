using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamingRequestHandlerLifetimeTests
{
    [Test]
    public async Task GivenTransientHandler_ResolvingHandlerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientHandlerWithFactory_ResolvingHandlerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler(p => new TestStreamingRequestHandler(p.GetRequiredService<TestObservations>()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

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

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
    }

    [Test]
    public async Task GivenScopedHandlerWithFactory_ResolvingHandlerCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler(p => new TestStreamingRequestHandler(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

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

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenSingletonHandlerWithFactory_ResolvingHandlerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler(p => new TestStreamingRequestHandler(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

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

        _ = services.AddSingleton<TestStreamingRequestHandlerWithMultipleInterfaces>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleInterfaces>(p => p.GetRequiredService<TestStreamingRequestHandlerWithMultipleInterfaces>(),
                                                                                                            ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonHandler_ResolvingHandlerViaConcreteClassReturnsSameInstanceAsResolvingViaInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<TestStreamingRequestHandler>();
        var handler2 = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonHandlerInstance_ResolvingHandlerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler(new TestStreamingRequestHandler(observations));

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed record TestStreamingRequest2;

    private sealed record TestItem2;

    private sealed class TestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamingRequestHandler(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
            yield return new();
        }
    }

    private sealed class TestStreamingRequestHandlerWithMultipleInterfaces : IStreamingRequestHandler<TestStreamingRequest, TestItem>,
                                                                             IStreamingRequestHandler<TestStreamingRequest2, TestItem2>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamingRequestHandlerWithMultipleInterfaces(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
            yield return new();
        }

        public async IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
            yield return new();
        }
    }

    private sealed class TestObservations
    {
        public List<int> InvocationCounts { get; } = new();
    }
}
