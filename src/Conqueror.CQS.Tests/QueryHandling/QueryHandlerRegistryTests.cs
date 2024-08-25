using Conqueror.CQS.QueryHandling;

namespace Conqueror.CQS.Tests.QueryHandling;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for dynamic type generation and assembly scanning to work")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "order makes sense, but some types must be private to not interfere with assembly scanning")]
public sealed class QueryHandlerRegistryTests
{
    [Test]
    public void GivenManuallyRegisteredQueryHandler_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandler_WhenRegisteringDifferentHandlerForSameQueryAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                              .AddConquerorQueryHandler<TestQueryHandler2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler2)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandler_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameQueryAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                              .AddConquerorQueryHandler<TestQueryHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandler_WhenRegisteringHandlerDelegateForSameQueryAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                              .AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromResult(new TestQueryResponse()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(DelegateQueryHandler<TestQuery, TestQueryResponse>)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandlerWithCustomInterface_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandlerWithCustomInterface_WhenRegisteringDifferentHandlerForSameQueryAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithCustomInterface>()
                                              .AddConquerorQueryHandler<TestQueryHandler2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler2)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandlerWithCustomInterface_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameQueryAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithCustomInterface>()
                                              .AddConquerorQueryHandler<TestQueryHandlerWithCustomInterface2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface2)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandlerWithCustomInterface_WhenRegisteringHandlerDelegateForSameQueryAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithCustomInterface>()
                                              .AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromResult(new TestQueryResponse()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(DelegateQueryHandler<TestQuery, TestQueryResponse>)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandlerDelegate_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromResult(new TestQueryResponse()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(DelegateQueryHandler<TestQuery, TestQueryResponse>)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandlerDelegate_WhenRegisteringDifferentHandlerForSameQueryAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromResult(new TestQueryResponse()))
                                              .AddConquerorQueryHandler<TestQueryHandler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandlerDelegate_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameQueryAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromResult(new TestQueryResponse()))
                                              .AddConquerorQueryHandler<TestQueryHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandlerDelegate_WhenRegisteringHandlerDelegateForSameQueryAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromResult(new TestQueryResponse()))
                                              .AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromResult(new TestQueryResponse()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(DelegateQueryHandler<TestQuery, TestQueryResponse>)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenMultipleManuallyRegisteredQueryHandlers_ReturnsRegistrations()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                              .AddConquerorQueryHandler<TestQuery2Handler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler)),
            new QueryHandlerRegistration(typeof(TestQuery2), typeof(TestQuery2Response), typeof(TestQuery2Handler)),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenQueryHandlersRegisteredViaAssemblyScanning_ReturnsRegistrations()
    {
        var provider = new ServiceCollection().AddConquerorCQSTypesFromExecutingAssembly()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Contains.Item(new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler)))
                                           .Or.Contains(new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface))));
        Assert.That(registrations, Contains.Item(new QueryHandlerRegistration(typeof(TestQuery2), typeof(TestQuery2Response), typeof(TestQuery2Handler))));
    }

    public sealed record TestQuery;

    public sealed record TestQueryResponse;

    public sealed record TestQuery2;

    public sealed record TestQuery2Response;

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
    }

    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    private sealed class TestQueryHandler2 : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    public sealed class TestQueryHandlerWithCustomInterface : ITestQueryHandler
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    private sealed class TestQueryHandlerWithCustomInterface2 : ITestQueryHandler
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    public sealed class TestQuery2Handler : IQueryHandler<TestQuery2, TestQuery2Response>
    {
        public Task<TestQuery2Response> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQuery2Response());
    }
}
