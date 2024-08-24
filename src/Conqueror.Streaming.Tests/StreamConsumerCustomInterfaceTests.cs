namespace Conqueror.Streaming.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for dynamic type generation to work")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "types must be public for dynamic type generation to work")]
public sealed class StreamConsumerCustomInterfaceTests
{
    [Test]
    public async Task GivenConsumerTypeWithCustomInterface_WhenCalledWithItem_ConsumerReceivesItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<ITestStreamConsumer>();

        var item = new TestItem();

        await consumer.HandleItem(item);

        Assert.That(observations.Items, Is.EquivalentTo(new[] { item }));
    }

    [Test]
    public async Task GivenGenericConsumerTypeWithCustomInterface_WhenCalledWithItem_ConsumerReceivesItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<GenericTestStreamConsumer<string>>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IGenericTestStreamConsumer<string>>();

        var item = new GenericTestItem<string>("test string");

        await consumer.HandleItem(item);

        Assert.That(observations.Items, Is.EquivalentTo(new[] { item }));
    }

    [Test]
    public async Task GivenConsumerTypeWithCustomInterface_WhenCalledWithCancellationToken_ConsumerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<ITestStreamConsumer>();
        using var tokenSource = new CancellationTokenSource();

        await consumer.HandleItem(new(), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenConsumerTypeWithCustomInterface_WhenCalledWithoutCancellationToken_ConsumerReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<ITestStreamConsumer>();

        await consumer.HandleItem(new());

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public void GivenConsumerTypeWithCustomInterface_WhenConsumerThrowsException_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorStreamConsumer<ThrowingStreamConsumer>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IThrowingStreamConsumer>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => consumer.HandleItem(new()));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenConsumerTypeWithCustomInterface_WhenResolvingPlainInterface_ReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var plainInterfaceConsumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();
        var customInterfaceConsumer = provider.GetRequiredService<ITestStreamConsumer>();

        await plainInterfaceConsumer.HandleItem(new());
        await customInterfaceConsumer.HandleItem(new());

        Assert.That(observations.Instances, Has.Count.EqualTo(2));
        Assert.That(observations.Instances[1], Is.SameAs(observations.Instances[0]));
    }

    [Test]
    public void GivenConsumerWithCustomInterface_ConsumerCanBeResolvedFromCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamConsumer>());
    }

    [Test]
    public void GivenConsumerWithCustomInterface_ConsumerCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamConsumer<TestItem>>());
    }

    [Test]
    public void GivenConsumerWithMultipleCustomInterfaces_ConsumerCanBeResolvedFromAllInterfaces()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleInterfaces>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamConsumer>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamConsumer2>());
    }

    [Test]
    public async Task GivenSingletonConsumerWithCustomInterfaces_ResolvingConsumerViaEitherInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleInterfaces>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer1 = provider.GetRequiredService<ITestStreamConsumer>();
        var consumer2 = provider.GetRequiredService<ITestStreamConsumer2>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());

        Assert.That(observations.Instances, Has.Count.EqualTo(2));
        Assert.That(observations.Instances[1], Is.SameAs(observations.Instances[0]));
    }

    [Test]
    public void GivenConsumerWithCustomInterfaceWithExtraMethods_RegisteringConsumerThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorStreamConsumer<TestStreamConsumerWithCustomInterfaceWithExtraMethod>());
    }

    [Test]
    public void GivenRegisteredConsumerTypeWithCustomInterface_WhenRegisteringDifferentConsumerTypeWithSameCustomInterface_ThrowsExceptionWithExplanation()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumer<TestStreamConsumer>();

        var thrownException = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamConsumer<DuplicateTestStreamConsumerForCustomInterface>());

        Assert.That(thrownException.Message, Is.EqualTo($"cannot add stream consumer type {typeof(DuplicateTestStreamConsumerForCustomInterface)} since a stream consumer type for item type {typeof(TestItem)} is already registered ({typeof(TestStreamConsumer)}); consider using keyed service registrations instead if you want multiple consumers for the same item type"));
    }

    [Test]
    public async Task GivenConsumerTypeWithCustomInterface_WhenRegisteringKeyed_CanBeResolvedAndCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed<TestStreamConsumer>(nameof(TestStreamConsumer))
                    .AddConquerorStreamConsumerKeyed<DuplicateTestStreamConsumerForCustomInterface>(nameof(DuplicateTestStreamConsumerForCustomInterface))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer1 = provider.GetRequiredKeyedService<ITestStreamConsumer>(nameof(TestStreamConsumer));
        var consumer2 = provider.GetRequiredKeyedService<ITestStreamConsumer>(nameof(DuplicateTestStreamConsumerForCustomInterface));

        var item = new TestItem();

        await consumer1.HandleItem(item);
        await consumer2.HandleItem(item);

        Assert.That(observations.Items, Is.EquivalentTo(new[] { item, item }));
        Assert.That(observations.Instances.Select(i => i.GetType()), Is.EquivalentTo(new[] { typeof(TestStreamConsumer), typeof(DuplicateTestStreamConsumerForCustomInterface) }));
    }

    public sealed record TestItem(int Payload = 10);

    public sealed record TestItem2;

    public sealed record GenericTestItem<T>(T Payload);

    public interface ITestStreamConsumer : IStreamConsumer<TestItem>;

    public interface ITestStreamConsumer2 : IStreamConsumer<TestItem2>;

    public interface IGenericTestStreamConsumer<T> : IStreamConsumer<GenericTestItem<T>>;

    public interface IThrowingStreamConsumer : IStreamConsumer<TestItem>;

    public interface ITestStreamConsumerWithExtraMethod : IStreamConsumer<TestItem>
    {
        void ExtraMethod();
    }

    private sealed class TestStreamConsumer(TestObservations observations) : ITestStreamConsumer
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Items.Add(item);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class DuplicateTestStreamConsumerForCustomInterface(TestObservations observations) : ITestStreamConsumer
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Items.Add(item);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class TestStreamConsumerWithMultipleInterfaces(TestObservations observations) : ITestStreamConsumer,
                                                                                                   ITestStreamConsumer2
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Items.Add(item);
            observations.CancellationTokens.Add(cancellationToken);
        }

        public async Task HandleItem(TestItem2 item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Items.Add(item);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class GenericTestStreamConsumer<T>(TestObservations observations) : IGenericTestStreamConsumer<T>
    {
        public async Task HandleItem(GenericTestItem<T> item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Items.Add(item);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class ThrowingStreamConsumer(Exception exception) : IThrowingStreamConsumer
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestStreamConsumerWithCustomInterfaceWithExtraMethod : ITestStreamConsumerWithExtraMethod
    {
        public Task HandleItem(TestItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestObservations
    {
        public List<object> Instances { get; } = new();

        public List<object> Items { get; } = new();

        public List<CancellationToken> CancellationTokens { get; } = new();
    }
}
