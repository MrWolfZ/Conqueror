namespace Conqueror.Streaming.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for dynamic type generation and assembly scanning to work")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "order makes sense, but some types must be private to not interfere with assembly scanning")]
public sealed class StreamProducerRegistryTests
{
    [Test]
    public void GivenManuallyRegisteredStreamProducer_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducer)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducer_WhenRegisteringDifferentProducerForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>()
                                              .AddConquerorStreamProducer<TestStreamProducer2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducer2)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducer_WhenRegisteringDifferentProducerWithCustomInterfaceForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>()
                                              .AddConquerorStreamProducer<TestStreamProducerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducerWithCustomInterface)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducer_WhenRegisteringProducerDelegateForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>()
                                              .AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(DelegateStreamProducer<TestStreamingRequest, TestItem>)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducerWithCustomInterface_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducerWithCustomInterface)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducerWithCustomInterface_WhenRegisteringDifferentProducerForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducerWithCustomInterface>()
                                              .AddConquerorStreamProducer<TestStreamProducer2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducer2)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducerWithCustomInterface_WhenRegisteringDifferentProducerWithCustomInterfaceForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducerWithCustomInterface>()
                                              .AddConquerorStreamProducer<TestStreamProducerWithCustomInterface2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducerWithCustomInterface2)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducerWithCustomInterface_WhenRegisteringProducerDelegateForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducerWithCustomInterface>()
                                              .AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(DelegateStreamProducer<TestStreamingRequest, TestItem>)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducerDelegate_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(DelegateStreamProducer<TestStreamingRequest, TestItem>)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducerDelegate_WhenRegisteringDifferentProducerForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .AddConquerorStreamProducer<TestStreamProducer>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducer)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducerDelegate_WhenRegisteringDifferentProducerWithCustomInterfaceForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .AddConquerorStreamProducer<TestStreamProducerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducerWithCustomInterface)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamProducerDelegate_WhenRegisteringProducerDelegateForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(DelegateStreamProducer<TestStreamingRequest, TestItem>)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenMultipleManuallyRegisteredStreamProducers_ReturnsRegistrations()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>()
                                              .AddConquerorStreamProducer<TestStreamingRequest2Producer>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducer)),
            new StreamProducerRegistration(typeof(TestStreamingRequest2), typeof(TestStreamingRequest2Response), typeof(TestStreamingRequest2Producer)),
        };

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenStreamProducersRegisteredViaAssemblyScanning_ReturnsRegistrations()
    {
        var provider = new ServiceCollection().AddConquerorStreamingTypesFromExecutingAssembly()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamProducerRegistry>();

        var registrations = registry.GetStreamProducerRegistrations();

        Assert.That(registrations, Contains.Item(new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducer)))
                                           .Or.Contains(new StreamProducerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamProducerWithCustomInterface))));
        Assert.That(registrations, Contains.Item(new StreamProducerRegistration(typeof(TestStreamingRequest2), typeof(TestStreamingRequest2Response), typeof(TestStreamingRequest2Producer))));
    }

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    public sealed record TestStreamingRequest2;

    public sealed record TestStreamingRequest2Response;

    public interface ITestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
    }

    public sealed class TestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());
    }

    private sealed class TestStreamProducer2 : IStreamProducer<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());
    }

    public sealed class TestStreamProducerWithCustomInterface : ITestStreamProducer
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());
    }

    private sealed class TestStreamProducerWithCustomInterface2 : ITestStreamProducer
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());
    }

    public sealed class TestStreamingRequest2Producer : IStreamProducer<TestStreamingRequest2, TestStreamingRequest2Response>
    {
        public IAsyncEnumerable<TestStreamingRequest2Response> ExecuteRequest(TestStreamingRequest2 request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestStreamingRequest2Response());
    }
}
