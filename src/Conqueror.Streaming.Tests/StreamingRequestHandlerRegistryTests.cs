namespace Conqueror.Streaming.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for dynamic type generation and assembly scanning to work")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "order makes sense, but some types must be private to not interfere with assembly scanning")]
public sealed class StreamingRequestHandlerRegistryTests
{
    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandler_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandler)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandler_WhenRegisteringDifferentHandlerForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                                              .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandler2)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandler_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                                              .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandlerWithCustomInterface)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandler_WhenRegisteringHandlerDelegateForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                                              .AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(DelegateStreamingRequestHandler<TestStreamingRequest, TestItem>)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandlerWithCustomInterface_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandlerWithCustomInterface)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandlerWithCustomInterface_WhenRegisteringDifferentHandlerForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCustomInterface>()
                                              .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandler2)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandlerWithCustomInterface_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCustomInterface>()
                                              .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCustomInterface2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandlerWithCustomInterface2)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandlerWithCustomInterface_WhenRegisteringHandlerDelegateForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCustomInterface>()
                                              .AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(DelegateStreamingRequestHandler<TestStreamingRequest, TestItem>)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandlerDelegate_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(DelegateStreamingRequestHandler<TestStreamingRequest, TestItem>)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandlerDelegate_WhenRegisteringDifferentHandlerForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandler)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandlerDelegate_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandlerWithCustomInterface)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredStreamingRequestHandlerDelegate_WhenRegisteringHandlerDelegateForSameRequestAndItemType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => AsyncEnumerableHelper.Of(new TestItem()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(DelegateStreamingRequestHandler<TestStreamingRequest, TestItem>)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenMultipleManuallyRegisteredStreamingRequestHandlers_ReturnsRegistrations()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                                              .AddConquerorStreamingRequestHandler<TestStreamingRequest2Handler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandler)),
            new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest2), typeof(TestStreamingRequest2Response), typeof(TestStreamingRequest2Handler)),
        };

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenStreamingRequestHandlersRegisteredViaAssemblyScanning_ReturnsRegistrations()
    {
        var provider = new ServiceCollection().AddConquerorStreamingTypesFromExecutingAssembly()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IStreamingRequestHandlerRegistry>();

        var registrations = registry.GetStreamingRequestHandlerRegistrations();

        Assert.That(registrations, Contains.Item(new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandler)))
                                           .Or.Contains(new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest), typeof(TestItem), typeof(TestStreamingRequestHandlerWithCustomInterface))));
        Assert.That(registrations, Contains.Item(new StreamingRequestHandlerRegistration(typeof(TestStreamingRequest2), typeof(TestStreamingRequest2Response), typeof(TestStreamingRequest2Handler))));
    }

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    public sealed record TestStreamingRequest2;

    public sealed record TestStreamingRequest2Response;

    public interface ITestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
    }

    public sealed class TestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());
    }

    private sealed class TestStreamingRequestHandler2 : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());
    }

    public sealed class TestStreamingRequestHandlerWithCustomInterface : ITestStreamingRequestHandler
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());
    }

    private sealed class TestStreamingRequestHandlerWithCustomInterface2 : ITestStreamingRequestHandler
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());
    }

    public sealed class TestStreamingRequest2Handler : IStreamingRequestHandler<TestStreamingRequest2, TestStreamingRequest2Response>
    {
        public IAsyncEnumerable<TestStreamingRequest2Response> ExecuteRequest(TestStreamingRequest2 request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestStreamingRequest2Response());
    }
}
