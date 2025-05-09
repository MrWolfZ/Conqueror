using Conqueror.CQS.CommandHandling;
using Conqueror.CQS.QueryHandling;

namespace Conqueror.CQS.Tests;

[TestFixture]
public sealed class RegistrationTests
{
    [Test]
    public void GivenServiceCollectionWithMultipleRegisteredHandlers_DoesNotRegisterConquerorTypesMultipleTimes()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                              .AddConquerorCommandHandler<TestCommand2Handler>()
                                              .AddConquerorQueryHandler<TestQueryHandler>()
                                              .AddConquerorQueryHandler<TestQuery2Handler>();

        Assert.That(services.Count(d => d.ServiceType == typeof(CommandClientFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ICommandClientFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(CommandTransportRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ICommandTransportRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(QueryClientFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IQueryClientFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(QueryTransportRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IQueryTransportRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IConquerorContextAccessor)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromExecutingAssemblyAddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
    {
        var services1 = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);
        var services2 = new ServiceCollection().AddConquerorCQSTypesFromExecutingAssembly();

        Assert.That(services2, Has.Count.EqualTo(services1.Count));
        Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsQueryHandlerWithPlainInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestQueryHandler) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsQueryHandlerWithCustomInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestQueryHandlerWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandHandlerWithPlainInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandHandler) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandHandlerWithCustomInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandHandlerWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandWithoutResponseHandlerWithPlainInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandWithoutResponseHandler) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCommandWithoutResponseHandlerWithCustomInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandWithoutResponseHandlerWithCustomInterface) &&
                                        d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollectionWithHandlerAlreadyRegistered_AddingAllTypesFromAssemblyDoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                              .AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestCommandHandler)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsInterfaces()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResponse>)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ITestQueryHandler)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ICommandHandler<TestCommand, TestCommandResponse>)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ITestCommandHandler)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ICommandHandler<TestCommandWithoutResponse>)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ITestCommandWithoutResponseHandler)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddAbstractClasses()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestQueryHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestCommandHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestCommandHandlerWithCustomInterface)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddGenericClasses()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestQueryHandler<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestCommandHandler<>)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddPrivateClasses()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestQueryHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestCommandHandler)));
    }

    public sealed record TestQuery;

    public sealed record TestQueryResponse;

    public sealed record TestQuery2;

    public sealed record TestQuery2Response;

    public sealed record TestQueryWithCustomInterface;

    public interface ITestQueryHandler : IQueryHandler<TestQueryWithCustomInterface, TestQueryResponse>;

    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    public sealed class TestQueryHandlerWithCustomInterface : ITestQueryHandler
    {
        public Task<TestQueryResponse> Handle(TestQueryWithCustomInterface query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    public sealed class TestQuery2Handler : IQueryHandler<TestQuery2, TestQuery2Response>
    {
        public Task<TestQuery2Response> Handle(TestQuery2 query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQuery2Response());
    }

    public abstract class AbstractTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    public sealed class GenericTestQueryHandler<T> : IQueryHandler<TestQuery, T>
        where T : new()
    {
        public Task<T> Handle(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new T());
    }

    private sealed class PrivateTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
    }

    public sealed record TestCommand;

    public sealed record TestCommandResponse;

    public sealed record TestCommand2;

    public sealed record TestCommand2Response;

    public sealed record TestCommandWithoutResponse;

    public sealed record TestCommandWithCustomInterface;

    public sealed record TestCommandWithoutResponseWithCustomInterface;

    public interface ITestCommandHandler : ICommandHandler<TestCommandWithCustomInterface, TestCommandResponse>;

    public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponseWithCustomInterface>;

    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
    }

    public sealed class TestCommandHandlerWithCustomInterface : ITestCommandHandler
    {
        public Task<TestCommandResponse> Handle(TestCommandWithCustomInterface command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
    }

    public sealed class TestCommand2Handler : ICommandHandler<TestCommand2, TestCommand2Response>
    {
        public Task<TestCommand2Response> Handle(TestCommand2 command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommand2Response());
    }

    public abstract class AbstractTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
    }

    public sealed class GenericTestCommandHandler<T> : ICommandHandler<TestCommand, T>
        where T : new()
    {
        public Task<T> Handle(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new T());
    }

    public abstract class AbstractTestCommandHandlerWithCustomInterface : ITestCommandHandler
    {
        public Task<TestCommandResponse> Handle(TestCommandWithCustomInterface command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
    }

    public sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
        public Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestCommandWithoutResponseHandlerWithCustomInterface : ITestCommandWithoutResponseHandler
    {
        public Task Handle(TestCommandWithoutResponseWithCustomInterface command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PrivateTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
    }
}
