namespace Conqueror.Streaming.Tests;

[TestFixture]
public sealed class StreamConsumerServiceCollectionConfigurationTests
{
    [Test]
    public void GivenRegisteredConsumerType_AddingIdenticalConsumerDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumer<TestStreamConsumer>();

        Assert.DoesNotThrow(() => services.AddConquerorStreamConsumer<TestStreamConsumer>());
    }

    [Test]
    public void GivenRegisteredConsumerType_AddingIdenticalConsumerOnlyKeepsOneRegistration()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumer<TestStreamConsumer>()
                                              .AddConquerorStreamConsumer<TestStreamConsumer>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestStreamConsumer)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredConsumerType_WhenAddingDifferentConsumerTypeWithSameItemType_ThrowsExceptionWithExplanation()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumer<TestStreamConsumer>();

        var thrownException = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamConsumer<DuplicateTestStreamConsumer>());

        Assert.That(thrownException.Message, Is.EqualTo($"cannot add stream consumer type {typeof(DuplicateTestStreamConsumer)} since a stream consumer type for item type {typeof(TestItem)} is already registered ({typeof(TestStreamConsumer)}); consider using keyed service registrations instead if you want multiple consumers for the same item type"));
    }

    [Test]
    public void GivenKeyedRegisteredConsumerType_AddingIdenticalKeyedConsumerKeepsBothRegistrations()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumerKeyed<TestStreamConsumer>(1)
                                              .AddConquerorStreamConsumerKeyed<TestStreamConsumer>(2);

        Assert.That(services.Count(s => s.ServiceType == typeof(TestStreamConsumer)), Is.EqualTo(2));
    }

    [Test]
    public void GivenRegisteredConsumerType_AddingIdenticalKeyedConsumerKeepsBothRegistrations()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumer<TestStreamConsumer>()
                                              .AddConquerorStreamConsumerKeyed<TestStreamConsumer>(1);

        Assert.That(services.Count(s => s.ServiceType == typeof(TestStreamConsumer)), Is.EqualTo(2));
    }

    [Test]
    public void GivenKeyedRegisteredConsumerType_AddingKeyedConsumerTypeForSameItemTypeKeepsBothRegistrations()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumerKeyed<TestStreamConsumer>(1)
                                              .AddConquerorStreamConsumerKeyed<DuplicateTestStreamConsumer>(2);

        Assert.That(services.Count(s => s.ServiceType == typeof(TestStreamConsumer)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(DuplicateTestStreamConsumer)), Is.EqualTo(1));
    }

    [Test]
    public void GivenUnregisteredConsumerType_WhenResolvingConsumer_ThrowsException()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorStreaming();

        var provider = services.BuildServiceProvider();

        var thrownException = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IStreamConsumer<TestItem>>());

        Assert.That(thrownException.Message, Does.StartWith("No service for type"));
    }

    [Test]
    public void GivenConsumerTypeRegisteredWithoutKey_WhenResolvingConsumerWithKey_ThrowsException()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>();

        var provider = services.BuildServiceProvider();

        var thrownException = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer)));

        Assert.That(thrownException.Message, Does.StartWith("No service for type"));
    }

    [Test]
    public void GivenKeyedConsumerType_WhenResolvingConsumerWithoutKey_ThrowsException()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorStreamConsumerKeyed<TestStreamConsumer>(nameof(TestStreamConsumer));

        var provider = services.BuildServiceProvider();

        var thrownException = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IStreamConsumer<TestItem>>());

        Assert.That(thrownException.Message, Does.StartWith("No service for type"));
    }

    private sealed record TestItem;

    private sealed class TestStreamConsumer : IStreamConsumer<TestItem>
    {
        public Task HandleItem(TestItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestStreamConsumer : IStreamConsumer<TestItem>
    {
        public Task HandleItem(TestItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
