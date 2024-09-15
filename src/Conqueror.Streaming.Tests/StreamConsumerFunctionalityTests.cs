namespace Conqueror.Streaming.Tests;

public sealed class StreamConsumerFunctionalityTests
{
    [Test]
    public async Task GivenStreamConsumer_WhenCalledWithItem_ConcreteConsumerIsCalledWithItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.Items, Is.EquivalentTo(new[] { item }));
    }

    [Test]
    public async Task GivenDelegateStreamConsumerCreatedViaFactory_WhenCalledWithItem_ConcreteConsumerIsCalledWithItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IStreamConsumerFactory>();

        var consumer = factory.Create<TestItem>(async (item, p, cancellationToken) =>
        {
            await Task.Yield();
            var obs = p.GetRequiredService<TestObservations>();
            obs.Items.Add(item);
            obs.CancellationTokens.Add(cancellationToken);
        });

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.Items, Is.EquivalentTo(new[] { item }));
    }

    [Test]
    public async Task GivenStreamConsumerForGenericItemCreatedViaFactory_WhenCalledWithItem_ConcreteConsumerIsCalledWithItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<GenericTestStreamConsumer<string>>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<GenericTestItem<string>>>();

        var item = new GenericTestItem<string>("test string");

        await consumer.HandleItem(item);

        Assert.That(observations.Items, Is.EquivalentTo(new[] { item }));
    }

    [Test]
    public async Task GivenDelegateStreamConsumerForGenericItemCreatedViaFactory_WhenCalledWithItem_ConcreteConsumerIsCalledWithItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IStreamConsumerFactory>();

        var consumer = factory.Create<GenericTestItem<string>>(async (item, p, cancellationToken) =>
        {
            await Task.Yield();
            var obs = p.GetRequiredService<TestObservations>();
            obs.Items.Add(item);
            obs.CancellationTokens.Add(cancellationToken);
        });

        var item = new GenericTestItem<string>("test string");

        await consumer.HandleItem(item);

        Assert.That(observations.Items, Is.EquivalentTo(new[] { item }));
    }

    [Test]
    public async Task GivenStreamConsumer_WhenCalledWithCancellationToken_ConcreteConsumerIsCalledWithCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        await consumer.HandleItem(new(10), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenDelegateStreamConsumerCreatedViaFactory_WhenCalledWithCancellationToken_ConcreteConsumerIsCalledWithCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IStreamConsumerFactory>();

        var consumer = factory.Create<TestItem>(async (item, p, cancellationToken) =>
        {
            await Task.Yield();
            var obs = p.GetRequiredService<TestObservations>();
            obs.Items.Add(item);
            obs.CancellationTokens.Add(cancellationToken);
        });

        using var tokenSource = new CancellationTokenSource();

        await consumer.HandleItem(new(10), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenStreamConsumer_WhenCalledWithoutCancellationToken_ConcreteConsumerIsCalledWithDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer.HandleItem(new(10));

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenDelegateStreamConsumerCreatedViaFactory_WhenCalledWithoutCancellationToken_ConcreteConsumerIsCalledWithDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IStreamConsumerFactory>();

        var consumer = factory.Create<TestItem>(async (item, p, cancellationToken) =>
        {
            await Task.Yield();
            var obs = p.GetRequiredService<TestObservations>();
            obs.Items.Add(item);
            obs.CancellationTokens.Add(cancellationToken);
        });

        await consumer.HandleItem(new(10));

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public void GivenStreamConsumer_WhenConsumerThrowsException_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exception = new Exception();

        _ = services.AddConquerorStreamConsumer<ThrowingTestStreamConsumer>()
                    .AddSingleton(observations)
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => consumer.HandleItem(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenDelegateStreamConsumerCreatedViaFactory_WhenConsumerThrowsException_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exception = new Exception();

        _ = services.AddConquerorStreaming()
                    .AddSingleton(observations)
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IStreamConsumerFactory>();

        var consumer = factory.Create<TestItem>(async (_, _, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var thrownException = Assert.ThrowsAsync<Exception>(() => consumer.HandleItem(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenKeyedStreamConsumers_WhenCalledWithItem_ConcreteConsumersAreCalledWithItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed<TestStreamConsumer>(nameof(TestStreamConsumer))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer2>(nameof(TestStreamConsumer2))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer1 = provider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer2 = provider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));

        var item = new TestItem(10);

        await consumer1.HandleItem(item);
        await consumer2.HandleItem(item);

        Assert.That(observations.Items, Is.EquivalentTo(new[] { item, item }));
        Assert.That(observations.Instances.Select(i => i.GetType()), Is.EquivalentTo(new[] { typeof(TestStreamConsumer), typeof(TestStreamConsumer2) }));
    }

    [Test]
    public async Task GivenDisposableHandler_WhenServiceProviderIsDisposed_ThenHandlerIsDisposed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<DisposableStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await handler.HandleItem(new(10), CancellationToken.None);

        await provider.DisposeAsync();

        Assert.That(observations.DisposedTypes, Is.EquivalentTo(new[] { typeof(DisposableStreamConsumer) }));
    }

    [Test]
    public void GivenStreamConsumerWithInvalidInterface_RegisteringConsumerThrowsArgumentException()
    {
        Assert.That(() => new ServiceCollection().AddConquerorStreamConsumer<TestStreamConsumerWithoutValidInterfaces>(), Throws.ArgumentException);
        Assert.That(() => new ServiceCollection().AddConquerorStreamConsumer<TestStreamConsumerWithoutValidInterfaces>(_ => new()), Throws.ArgumentException);
        Assert.That(() => new ServiceCollection().AddConquerorStreamConsumer(new TestStreamConsumerWithoutValidInterfaces()), Throws.ArgumentException);
    }

    private sealed record TestItem(int Payload);

    private sealed record GenericTestItem<T>(T Payload);

    private sealed class TestStreamConsumer(TestObservations observations) : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Items.Add(item);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class TestStreamConsumer2(TestObservations observations) : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Items.Add(item);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class GenericTestStreamConsumer<T>(TestObservations observations) : IStreamConsumer<GenericTestItem<T>>
    {
        public async Task HandleItem(GenericTestItem<T> item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Items.Add(item);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class ThrowingTestStreamConsumer(Exception exception) : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class DisposableStreamConsumer(TestObservations observations) : IStreamConsumer<TestItem>, IDisposable
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public void Dispose()
        {
            observations.DisposedTypes.Add(GetType());
        }
    }

    private sealed class TestStreamConsumerWithoutValidInterfaces : IStreamConsumer;

    private sealed class TestObservations
    {
        public List<object> Instances { get; } = [];

        public List<object> Items { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public List<Type> DisposedTypes { get; } = [];
    }
}
