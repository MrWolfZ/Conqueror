namespace Conqueror.Streaming.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for assembly scanning to work")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "for testing purposes we want to mix public and private classes")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "types must be public for assembly scanning to work")]
public sealed class RegistrationTests
{
    [Test]
    public void GivenServiceCollectionWithMultipleRegisteredProducers_DoesNotRegisterConquerorTypesMultipleTimes()
    {
        var services = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>()
                                              .AddConquerorStreamProducer<TestStreamProducer2>();

        Assert.That(services.Count(d => d.ServiceType == typeof(StreamProducerClientFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IStreamProducerClientFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(StreamProducerRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IStreamProducerRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(StreamProducerMiddlewareRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IConquerorContextAccessor)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollectionWithMultipleRegisteredConsumers_DoesNotRegisterConquerorTypesMultipleTimes()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumer<TestStreamConsumer>()
                                              .AddConquerorStreamConsumer<TestStreamConsumer2>();

        Assert.That(services.Count(d => d.ServiceType == typeof(IStreamConsumerFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(StreamConsumerFactory)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(StreamConsumerMiddlewareRegistry)), Is.EqualTo(1));
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
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamProducerWithPlainInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamProducer) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamConsumerWithPlainInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamConsumer) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamProducerWithCustomInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamProducerWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamConsumerWithCustomInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamConsumerWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamProducerMiddlewareAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamProducerMiddleware) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamConsumerMiddlewareAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamConsumerMiddleware) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamProducerMiddlewareWithoutConfigurationAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamProducerMiddlewareWithoutConfiguration) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamConsumerMiddlewareWithoutConfigurationAsTransient()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamConsumerMiddlewareWithoutConfiguration) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollectionWithProducerAlreadyRegistered_AddingAllTypesFromAssemblyDoesNotAddProducerAgain()
    {
        var services = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>(ServiceLifetime.Singleton)
                                              .AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamProducer)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollectionWithConsumerAlreadyRegistered_AddingAllTypesFromAssemblyDoesNotAddConsumerAgain()
    {
        var services = new ServiceCollection().AddConquerorStreamConsumer<TestStreamConsumer>(ServiceLifetime.Singleton)
                                              .AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamConsumer)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsInterfaces()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ServiceType == typeof(IStreamProducer<TestStreamingRequest, TestItem>)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ITestStreamProducer)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(ITestStreamConsumer)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddAbstractClasses()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestStreamProducer)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestStreamProducerWithCustomInterface)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestStreamProducerMiddleware)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestStreamConsumer)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestStreamConsumerWithCustomInterface)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestStreamConsumerMiddleware)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddGenericClasses()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestStreamProducer<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestStreamProducerMiddleware<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestStreamConsumer<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestStreamConsumerMiddleware<>)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddPrivateClasses()
    {
        var services = new ServiceCollection().AddConquerorStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestStreamProducer)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestStreamProducerMiddleware)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestStreamConsumer)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestStreamConsumerMiddleware)));
    }

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    public sealed record TestStreamingRequest2;

    public sealed record TestItem2;

    public sealed record TestStreamingRequestWithCustomInterface;

    public sealed record TestItemWithCustomInterface;

    public interface ITestStreamProducer : IStreamProducer<TestStreamingRequestWithCustomInterface, TestItem>;

    public sealed class TestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    public sealed class TestStreamProducerWithCustomInterface : ITestStreamProducer
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithCustomInterface request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    public sealed class TestStreamProducer2 : IStreamProducer<TestStreamingRequest2, TestItem2>
    {
        public IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem2>();
    }

    public abstract class AbstractTestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    public sealed class GenericTestStreamProducer<T> : IStreamProducer<TestStreamingRequest, T>
        where T : new()
    {
        public IAsyncEnumerable<T> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<T>();
    }

    public abstract class AbstractTestStreamProducerWithCustomInterface : ITestStreamProducer
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithCustomInterface request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    private sealed class PrivateTestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Empty<TestItem>();
    }

    public sealed class TestStreamProducerMiddlewareConfiguration;

    public sealed class TestStreamProducerMiddleware : IStreamProducerMiddleware<TestStreamProducerMiddlewareConfiguration>
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem, TestStreamProducerMiddlewareConfiguration> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }

    public sealed class TestStreamProducerMiddlewareWithoutConfiguration : IStreamProducerMiddleware
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }

    public abstract class AbstractTestStreamProducerMiddleware : IStreamProducerMiddleware<TestStreamProducerMiddlewareConfiguration>
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem, TestStreamProducerMiddlewareConfiguration> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }

    public sealed class GenericTestStreamProducerMiddleware<T> : IStreamProducerMiddleware<T>
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem, T> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }

    private sealed class PrivateTestStreamProducerMiddleware : IStreamProducerMiddleware<TestStreamProducerMiddlewareConfiguration>
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem, TestStreamProducerMiddlewareConfiguration> ctx)
            where TRequest : class =>
            ctx.Next(ctx.Request, ctx.CancellationToken);
    }

    public interface ITestStreamConsumer : IStreamConsumer<TestItemWithCustomInterface>;

    public sealed class TestStreamConsumer : IStreamConsumer<TestItem>
    {
        public Task HandleItem(TestItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestStreamConsumerWithCustomInterface : ITestStreamConsumer
    {
        public Task HandleItem(TestItemWithCustomInterface item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestStreamConsumer2 : IStreamConsumer<TestItem2>
    {
        public Task HandleItem(TestItem2 item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public abstract class AbstractTestStreamConsumer : IStreamConsumer<TestItem>
    {
        public Task HandleItem(TestItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class GenericTestStreamConsumer<T> : IStreamConsumer<T>
        where T : new()
    {
        public Task HandleItem(T item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public abstract class AbstractTestStreamConsumerWithCustomInterface : ITestStreamConsumer
    {
        public Task HandleItem(TestItemWithCustomInterface item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PrivateTestStreamConsumer : IStreamConsumer<TestItem>
    {
        public Task HandleItem(TestItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestStreamConsumerMiddlewareConfiguration;

    public sealed class TestStreamConsumerMiddleware : IStreamConsumerMiddleware<TestStreamConsumerMiddlewareConfiguration>
    {
        public Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem, TestStreamConsumerMiddlewareConfiguration> ctx)
            => ctx.Next(ctx.Item, ctx.CancellationToken);
    }

    public sealed class TestStreamConsumerMiddlewareWithoutConfiguration : IStreamConsumerMiddleware
    {
        public Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
            => ctx.Next(ctx.Item, ctx.CancellationToken);
    }

    public abstract class AbstractTestStreamConsumerMiddleware : IStreamConsumerMiddleware<TestStreamConsumerMiddlewareConfiguration>
    {
        public Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem, TestStreamConsumerMiddlewareConfiguration> ctx)
            => ctx.Next(ctx.Item, ctx.CancellationToken);
    }

    public sealed class GenericTestStreamConsumerMiddleware<T> : IStreamConsumerMiddleware<T>
    {
        public Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem, T> ctx)
            => ctx.Next(ctx.Item, ctx.CancellationToken);
    }

    private sealed class PrivateTestStreamConsumerMiddleware : IStreamConsumerMiddleware<TestStreamConsumerMiddlewareConfiguration>
    {
        public Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem, TestStreamConsumerMiddlewareConfiguration> ctx)
            => ctx.Next(ctx.Item, ctx.CancellationToken);
    }
}
