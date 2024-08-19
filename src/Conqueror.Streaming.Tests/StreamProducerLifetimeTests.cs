using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamProducerLifetimeTests
{
    [Test]
    public async Task GivenTransientProducer_ResolvingProducerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientProducerWithFactory_ResolvingProducerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer(p => new TestStreamProducer(p.GetRequiredService<TestObservations>()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedProducer_ResolvingProducerCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
    }

    [Test]
    public async Task GivenScopedProducerWithFactory_ResolvingProducerCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer(p => new TestStreamProducer(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
    }

    [Test]
    public async Task GivenSingletonProducer_ResolvingProducerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenSingletonProducerWithFactory_ResolvingProducerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer(p => new TestStreamProducer(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenSingletonProducerWithMultipleProducerInterfaces_ResolvingProducerViaEitherInterfaceReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSingleton<TestStreamProducerWithMultipleInterfaces>()
                    .AddConquerorStreamProducer<TestStreamProducerWithMultipleInterfaces>(p => p.GetRequiredService<TestStreamProducerWithMultipleInterfaces>(),
                                                                                          ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer1 = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = provider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonProducer_ResolvingProducerViaConcreteClassReturnsSameInstanceAsResolvingViaInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer1 = provider.GetRequiredService<TestStreamProducer>();
        var producer2 = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonProducerInstance_ResolvingProducerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer(new TestStreamProducer(observations));

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed record TestStreamingRequest2;

    private sealed record TestItem2;

    private sealed class TestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamProducer(TestObservations observations)
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

    private sealed class TestStreamProducerWithMultipleInterfaces : IStreamProducer<TestStreamingRequest, TestItem>,
                                                                    IStreamProducer<TestStreamingRequest2, TestItem2>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamProducerWithMultipleInterfaces(TestObservations observations)
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
