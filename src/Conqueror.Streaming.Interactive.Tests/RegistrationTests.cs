namespace Conqueror.Streaming.Interactive.Tests;

[TestFixture]
public sealed class RegistrationTests
{
    [Test]
    public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
    {
        var services = new ServiceCollection().AddConquerorInteractiveStreaming().AddConquerorInteractiveStreaming();

        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(InteractiveStreamingHandlerRegistry)));
        //// TODO
        //// Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(InteractiveStreamingMiddlewaresInvoker)));
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(InteractiveStreamingRegistrationFinalizer)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromExecutingAssemblyAddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
    {
        var services1 = new ServiceCollection().AddConquerorInteractiveStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);
        var services2 = new ServiceCollection().AddConquerorInteractiveStreamingTypesFromExecutingAssembly();

        Assert.AreEqual(services1.Count, services2.Count);
        Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamingHandlerWithPlainInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorInteractiveStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamingHandler) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsStreamingHandlerWithCustomInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorInteractiveStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamingHandlerWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
    }

    // TODO
    // [Test]
    // public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsMiddlewareAsTransient()
    // {
    //     var services = new ServiceCollection().AddConquerorInteractiveStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);
    //
    //     Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestInteractiveStreamingMiddleware) && d.Lifetime == ServiceLifetime.Transient));
    // }
    //
    // [Test]
    // public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsMiddlewareWithoutConfigurationAsTransient()
    // {
    //     var services = new ServiceCollection().AddConquerorInteractiveStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);
    //
    //     Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestInteractiveStreamingMiddlewareWithoutConfiguration) && d.Lifetime == ServiceLifetime.Transient));
    // }

    [Test]
    public void GivenServiceCollectionWithHandlerAlreadyRegistered_AddingAllTypesFromAssemblyDoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddSingleton<TestStreamingHandler>().AddConquerorInteractiveStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.AreEqual(1, services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestStreamingHandler)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddInterfaces()
    {
        var services = new ServiceCollection().AddSingleton<TestStreamingHandler>().AddConquerorInteractiveStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.IsFalse(services.Any(d => d.ServiceType == typeof(ITestStreamingHandler)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddAbstractClasses()
    {
        var services = new ServiceCollection().AddSingleton<TestStreamingHandler>().AddConquerorInteractiveStreamingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestStreamingHandler)));
    }

    [Test]
    public void GivenServiceCollectionWithConquerorInteractiveStreamingRegistrationWithoutFinalization_ThrowsExceptionWhenBuildingServiceProviderWithValidation()
    {
        var services = new ServiceCollection().AddConquerorInteractiveStreaming();

        var ex = Assert.Throws<AggregateException>(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));

        Assert.IsInstanceOf<InvalidOperationException>(ex?.InnerException);
        Assert.That(ex?.InnerException?.Message, Contains.Substring("DidYouForgetToCallFinalizeConquerorRegistrations"));
    }

    [Test]
    public void GivenServiceCollectionWithConquerorInteractiveStreamingRegistrationWithFinalization_ThrowsExceptionWhenCallingFinalizationAgain()
    {
        var services = new ServiceCollection().AddConquerorInteractiveStreaming().FinalizeConquerorRegistrations();

        _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
    }

    [Test]
    public void GivenServiceCollectionWithFinalization_ThrowsExceptionWhenRegisteringInteractiveStreaming()
    {
        var services = new ServiceCollection().FinalizeConquerorRegistrations();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorInteractiveStreaming());
    }

    private sealed record TestRequest;

    private sealed record TestItem;

    private interface ITestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
    {
    }

    private sealed class TestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class TestStreamingHandlerWithCustomInterface : ITestStreamingHandler
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private abstract class AbstractTestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    // TODO
    // private sealed class TestInteractiveStreamingMiddlewareConfiguration
    // {
    // }
    //
    // private sealed class TestInteractiveStreamingMiddleware : IInteractiveStreamingMiddleware<TestInteractiveStreamingMiddlewareConfiguration>
    // {
    //     public Task<TItem> Execute<TRequest, TItem>(InteractiveStreamingMiddlewareContext<TRequest, TItem, TestInteractiveStreamingMiddlewareConfiguration> ctx)
    //         where TRequest : class =>
    //         ctx.Next(ctx.Request, ctx.CancellationToken);
    // }
    //
    // private sealed class TestInteractiveStreamingMiddlewareWithoutConfiguration : IInteractiveStreamingMiddleware
    // {
    //     public Task<TItem> Execute<TRequest, TItem>(InteractiveStreamingMiddlewareContext<TRequest, TItem> ctx)
    //         where TRequest : class =>
    //         ctx.Next(ctx.Request, ctx.CancellationToken);
    // }
}
