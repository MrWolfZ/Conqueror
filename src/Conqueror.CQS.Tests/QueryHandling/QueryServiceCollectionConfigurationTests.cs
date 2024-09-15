namespace Conqueror.CQS.Tests.QueryHandling;

[TestFixture]
public sealed class QueryServiceCollectionConfigurationTests
{
    [Test]
    public void GivenRegisteredHandlerType_AddingIdenticalHandlerDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>();

        Assert.DoesNotThrow(() => services.AddConquerorQueryHandler<TestQueryHandler>());
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingIdenticalHandlerOnlyKeepsOneRegistration()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                              .AddConquerorQueryHandler<TestQueryHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestQueryHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResponse>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingHandlerWithSameQueryAndResponseTypesKeepsBothRegistrations()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                              .AddConquerorQueryHandler<DuplicateTestQueryHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestQueryHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(DuplicateTestQueryHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResponse>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingHandlerWithSameQueryTypeAndDifferentResponseTypeThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryHandler<DuplicateTestQueryHandlerWithDifferentResponseType>());
    }

    [Test]
    public void GivenHandlerTypeWithInstanceFactory_AddedHandlerCanBeResolvedFromInterface()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler(_ => new TestQueryHandler())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    private sealed record TestQueryResponse2;

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestQueryHandlerWithDifferentResponseType : IQueryHandler<TestQuery, TestQueryResponse2>
    {
        public Task<TestQueryResponse2> Handle(TestQuery command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
