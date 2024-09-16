using Conqueror.CQS.QueryHandling;

namespace Conqueror.CQS.Tests.QueryHandling;

[TestFixture]
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler), null),
        };

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredQueryHandlerWithPipeline_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithPipeline>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

        var registrations = registry.GetQueryHandlerRegistrations();

        Assert.Multiple(() =>
        {
            Assert.That(registrations.Single().QueryType, Is.EqualTo(typeof(TestQuery)));
            Assert.That(registrations.Single().ResponseType, Is.EqualTo(typeof(TestQueryResponse)));
            Assert.That(registrations.Single().HandlerType, Is.EqualTo(typeof(TestQueryHandlerWithPipeline)));
            Assert.That(registrations.Single().ConfigurePipeline, Is.Not.Null);
        });
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler2), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(DelegateQueryHandler<TestQuery, TestQueryResponse>), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler2), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface2), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(DelegateQueryHandler<TestQuery, TestQueryResponse>), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(DelegateQueryHandler<TestQuery, TestQueryResponse>), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(DelegateQueryHandler<TestQuery, TestQueryResponse>), null),
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
            new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler), null),
            new QueryHandlerRegistration(typeof(TestQuery2), typeof(TestQuery2Response), typeof(TestQuery2Handler), null),
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

        Assert.That(registrations, Contains.Item(new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler), null))
                                           .Or.Contains(new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface), null)));
        Assert.That(registrations, Contains.Item(new QueryHandlerRegistration(typeof(TestQuery2), typeof(TestQuery2Response), typeof(TestQuery2Handler), null)));
    }

    [Test]
    public void GivenHandlerWithInvalidInterface_RegisteringHandlerThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithoutValidInterfaces>(_ => new()));
    }

    [Test]
    public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorQueryHandler<TestQueryHandlerWithCustomInterfaceWithExtraMethod>());
    }

    [Test]
    public void GivenHandlerWithMultipleCustomInterfaces_HandlerCanBeResolvedFromAllInterfaces()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithMultipleInterfaces>();

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery2, TestQuery2Response>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler2>());
    }

    public sealed record TestQuery;

    public sealed record TestQueryResponse;

    public sealed record TestQuery2;

    public sealed record TestQuery2Response;

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;

    public interface ITestQueryHandler2 : IQueryHandler<TestQuery2, TestQuery2Response>;

    public interface ITestQueryHandlerWithExtraMethod : IQueryHandler<TestQuery, TestQueryResponse>
    {
        void ExtraMethod();
    }

    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    private sealed class TestQueryHandler2 : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    public sealed class TestQueryHandlerWithCustomInterface : ITestQueryHandler
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    private sealed class TestQueryHandlerWithCustomInterface2 : ITestQueryHandler
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    public sealed class TestQuery2Handler : IQueryHandler<TestQuery2, TestQuery2Response>
    {
        public Task<TestQuery2Response> Handle(TestQuery2 query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQuery2Response());
    }

    private sealed class TestQueryHandlerWithCustomInterfaceWithExtraMethod : ITestQueryHandlerWithExtraMethod
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestQueryHandlerWithMultipleInterfaces : ITestQueryHandler,
                                                                  ITestQueryHandler2
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TestQuery2Response> Handle(TestQuery2 query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestQueryHandlerWithPipeline : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());

        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline)
        {
        }
    }

    private sealed class TestQueryHandlerWithoutValidInterfaces : IQueryHandler;
}
