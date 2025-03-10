using Conqueror.CQS.QueryHandling;

namespace Conqueror.CQS.Tests.QueryHandling;

[TestFixture]
public sealed class QueryHandlerRegistrationTests
{
    [Test]
    [Combinatorial]
    public void GivenRegisteredHandlers_WhenCallingRegistry_ReturnsCorrectRegistrations(
        [Values("type", "factory", "instance", "delegate")]
        string registrationMethod)
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddConquerorQueryHandler<TestQueryHandler>()
                              .AddConquerorQueryHandler<TestQuery2Handler>(),
            "factory" => services.AddConquerorQueryHandler(_ => new TestQueryHandler())
                                 .AddConquerorQueryHandler(_ => new TestQuery2Handler()),
            "instance" => services.AddConquerorQueryHandler(new TestQueryHandler())
                                  .AddConquerorQueryHandler(new TestQuery2Handler()),
            "delegate" => services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => throw new NotSupportedException())
                                  .AddConquerorQueryHandlerDelegate<TestQuery2, TestQuery2Response>((_, _, _) => throw new NotSupportedException()),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, null),
        };

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestQuery), typeof(TestQueryResponse), new InProcessQueryAttribute()),
            (typeof(TestQuery2), typeof(TestQuery2Response), new()),
        };

        var registrations = registry.GetQueryTypesForTransport<InProcessQueryAttribute>();

        Assert.That(registrations, Is.EqualTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandler_WhenRegisteringSameHandlerDifferently_OverwritesRegistration(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance")]
        string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance")]
        string overwrittenRegistrationMethod)
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestQueryHandler> factory = _ => new();
        var instance = new TestQueryHandler();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddConquerorQueryHandler<TestQueryHandler>(),
                (null, "factory") => services.AddConquerorQueryHandler(factory),
                (var l, "type") => services.AddConquerorQueryHandler<TestQueryHandler>(l.Value),
                (var l, "factory") => services.AddConquerorQueryHandler(factory, l.Value),
                (_, "instance") => services.AddConquerorQueryHandler(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(services.Count(s => s.ServiceType == typeof(TestQueryHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResponse>)), Is.EqualTo(1));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestQueryHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestQueryHandler)).ImplementationType, Is.EqualTo(typeof(TestQueryHandler)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestQueryHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestQueryHandler)).ImplementationFactory, Is.SameAs(factory));
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestQueryHandler)).ImplementationInstance, Is.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null);
        }

        using var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());

        var registry = provider.GetRequiredService<IQueryTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestQuery), typeof(TestQueryResponse), new InProcessQueryAttribute()),
        };

        var registrations = registry.GetQueryTypesForTransport<InProcessQueryAttribute>();

        Assert.That(registrations, Is.EqualTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandler_WhenRegisteringDifferentHandlerForSameQueryType_ThrowsInvalidOperationException(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string overwrittenRegistrationMethod)
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestQueryHandler> factory = _ => new();
        Func<IServiceProvider, DuplicateTestQueryHandler> duplicateFactory = _ => new();
        var instance = new TestQueryHandler();
        var duplicateInstance = new DuplicateTestQueryHandler();

        _ = (initialLifetime, initialRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorQueryHandler<TestQueryHandler>(),
            (null, "factory") => services.AddConquerorQueryHandler(factory),
            (var l, "type") => services.AddConquerorQueryHandler<TestQueryHandler>(l.Value),
            (var l, "factory") => services.AddConquerorQueryHandler(factory, l.Value),
            (_, "instance") => services.AddConquerorQueryHandler(instance),
            (_, "delegate") => services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => throw new NotSupportedException()),
            _ => throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null),
        };

        _ = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = (overwrittenLifetime, overwrittenRegistrationMethod) switch
            {
                (null, "type") => services.AddConquerorQueryHandler<DuplicateTestQueryHandler>(),
                (null, "factory") => services.AddConquerorQueryHandler(duplicateFactory),
                (var l, "type") => services.AddConquerorQueryHandler<DuplicateTestQueryHandler>(l.Value),
                (var l, "factory") => services.AddConquerorQueryHandler(duplicateFactory, l.Value),
                (_, "instance") => services.AddConquerorQueryHandler(duplicateInstance),
                (_, "delegate") => services.AddConquerorQueryHandlerDelegate<TestQuery, TestQuery2Response>((_, _, _) => throw new NotSupportedException()),
                _ => throw new ArgumentOutOfRangeException(nameof(overwrittenRegistrationMethod), overwrittenRegistrationMethod, null),
            };
        });
    }

    [Test]
    public void GivenRegisteredHandlerType_WhenRegisteringHandlersViaAssemblyScanning_DoesNotOverwriteRegistration()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerForAssemblyScanning>(ServiceLifetime.Singleton)
                                              .AddConquerorCQSTypesFromExecutingAssembly();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestQueryHandlerForAssemblyScanning)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IQueryHandler<TestQueryForAssemblyScanning, TestQueryResponseForAssemblyScanning>)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestQueryHandlerForAssemblyScanning)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IQueryTransportRegistry>().GetQueryTypesForTransport<InProcessQueryAttribute>();

        Assert.That(registrations, Has.One.EqualTo((typeof(TestQueryForAssemblyScanning), typeof(TestQueryResponseForAssemblyScanning), new InProcessQueryAttribute())));
    }

    [Test]
    public void GivenServiceCollection_WhenRegisteringHandlersViaAssemblyScanningMultipleTimes_DoesNotOverwriteRegistrations()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromExecutingAssembly()
                                              .AddConquerorCQSTypesFromExecutingAssembly();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestQueryHandlerForAssemblyScanning)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IQueryHandler<TestQueryForAssemblyScanning, TestQueryResponseForAssemblyScanning>)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestQueryHandlerForAssemblyScanning)).Lifetime, Is.EqualTo(ServiceLifetime.Transient));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IQueryTransportRegistry>().GetQueryTypesForTransport<InProcessQueryAttribute>();

        Assert.That(registrations, Has.One.EqualTo((typeof(TestQueryForAssemblyScanning), typeof(TestQueryResponseForAssemblyScanning), new InProcessQueryAttribute())));
    }

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    private sealed record TestQuery2;

    private sealed record TestQuery2Response;

    public sealed record TestQueryForAssemblyScanning;

    public sealed record TestQueryResponseForAssemblyScanning;

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestQuery2Handler : IQueryHandler<TestQuery2, TestQuery2Response>
    {
        public Task<TestQuery2Response> Handle(TestQuery2 query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestQueryHandler : IQueryHandler<TestQuery, TestQuery2Response>
    {
        public Task<TestQuery2Response> Handle(TestQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public sealed class TestQueryHandlerForAssemblyScanning : IQueryHandler<TestQueryForAssemblyScanning, TestQueryResponseForAssemblyScanning>
    {
        public Task<TestQueryResponseForAssemblyScanning> Handle(TestQueryForAssemblyScanning query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
