namespace Conqueror.Streaming.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for assembly scanning to work")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "for testing purposes we want to mix public and private classes")]
public sealed class RegistrationTests
{
    [Test]
    public void GivenServiceCollectionWithMultipleRegisteredHandlers_DoesNotRegisterConquerorTypesMultipleTimes()
    {
        var services = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                                              .AddConquerorStreamingRequestHandler<TestStreamingRequest2Handler>();

        Assert.That(services.Count(d => d.ServiceType == typeof(StreamingRequestClientFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IStreamingRequestClientFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(StreamingRequestHandlerRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IStreamingRequestHandlerRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(StreamingRequestMiddlewareRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IConquerorContextAccessor)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromExecutingAssemblyAddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
    {
        var services1 = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);
        var services2 = new ServiceCollection().AddConquerorStreamingTypesFromExecutingAssembly();

        Assert.That(services2, Has.Count.EqualTo(services1.Count));
        Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamingRequestHandlerWithPlainInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamingRequestHandler) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamingRequestHandlerWithCustomInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamingRequestHandlerWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamingRequestMiddlewareAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamingRequestMiddleware) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamingRequestMiddlewareWithoutConfigurationAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamingRequestMiddlewareWithoutConfiguration) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollectionWithHandlerAlreadyRegistered_AddingAllTypesFromAssemblyDoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>(ServiceLifetime.Singleton)
                                              .AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamingRequestHandler)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsInterfaces()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ServiceType == typeof(IStreamingRequestHandler<TestStreamingRequest, TestItem>)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ITestStreamingRequestHandler)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddAbstractClasses()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestStreamingRequestHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestStreamingRequestHandlerWithCustomInterface)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestStreamingRequestMiddleware)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddGenericClasses()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestStreamingRequestHandler<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestStreamingRequestMiddleware<>)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddPrivateClasses()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestStreamingRequestHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestStreamingRequestMiddleware)));
    }

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    public sealed record TestStreamingRequest2;

    public sealed record TestItem2;

    public sealed record TestStreamingRequestWithCustomInterface;

    public interface ITestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequestWithCustomInterface, TestItem>
    {
    }

    public sealed class TestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    public sealed class TestStreamingRequestHandlerWithCustomInterface : ITestStreamingRequestHandler
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithCustomInterface request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    public sealed class TestStreamingRequest2Handler : IStreamingRequestHandler<TestStreamingRequest2, TestItem2>
    {
        public IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem2>();
    }

    public abstract class AbstractTestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    public sealed class GenericTestStreamingRequestHandler<T> : IStreamingRequestHandler<TestStreamingRequest, T>
        where T : new()
    {
        public IAsyncEnumerable<T> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<T>();
    }

    public abstract class AbstractTestStreamingRequestHandlerWithCustomInterface : ITestStreamingRequestHandler
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithCustomInterface request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    private sealed class PrivateTestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    public sealed class TestStreamingRequestMiddlewareConfiguration
    {
    }

    public sealed class TestStreamingRequestMiddleware : IStreamingRequestMiddleware<TestStreamingRequestMiddlewareConfiguration>
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem, TestStreamingRequestMiddlewareConfiguration> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }

    public sealed class TestStreamingRequestMiddlewareWithoutConfiguration : IStreamingRequestMiddleware
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }

    public abstract class AbstractTestStreamingRequestMiddleware : IStreamingRequestMiddleware<TestStreamingRequestMiddlewareConfiguration>
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem, TestStreamingRequestMiddlewareConfiguration> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }

    public sealed class GenericTestStreamingRequestMiddleware<T> : IStreamingRequestMiddleware<T>
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem, T> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }

    private sealed class PrivateTestStreamingRequestMiddleware : IStreamingRequestMiddleware<TestStreamingRequestMiddlewareConfiguration>
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem, TestStreamingRequestMiddlewareConfiguration> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }
}
