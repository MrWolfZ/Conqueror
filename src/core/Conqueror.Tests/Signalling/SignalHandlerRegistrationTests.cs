namespace Conqueror.Tests.Signalling;

[TestFixture]
public sealed partial class SignalHandlerRegistrationTests
{
    [Test]
    public void GivenServiceCollection_WhenRegisteringMultipleHandlers_DoesNotRegisterConquerorTypesMultipleTimes()
    {
        var services = new ServiceCollection()
            .AddSignalHandler<TestSignalHandler>()
            .AddSignalHandler<TestSignal2Handler>();

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ISignalDispatcher))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ISignalPublishers))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IInProcessSignalPublisherFactory))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IAggregateSignalPublisherFactory))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ISignalIdFactory))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(SignalHandlerRegistry))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ISignalHandlerRegistry))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(InProcessSignalReceiver))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IConquerorContextAccessor))
        );
    }

    [Test]
    [Combinatorial]
    public void GivenServiceCollection_WhenAddingSignalHandlers_AddsCorrectHandlerRegistrations(
        [Values("type", "factory", "instance", "delegate", "sync_delegate")] string registrationMethod
    )
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddSignalHandler<TestSignalHandler>().AddSignalHandler<TestSignal2Handler>(),
            "factory" => services
                .AddSignalHandler(_ => new TestSignalHandler())
                .AddSignalHandler(_ => new TestSignal2Handler()),
            "instance" => services.AddSignalHandler(new TestSignalHandler()).AddSignalHandler(new TestSignal2Handler()),
            "delegate" => services
                .AddSignalHandlerDelegate(TestSignal.T, (_, _, _) => Task.CompletedTask)
                .AddSignalHandlerDelegate(TestSignal2.T, (_, _, _) => Task.CompletedTask),
            "sync_delegate" => services
                .AddSignalHandlerDelegate(TestSignal.T, (_, _) => { })
                .AddSignalHandlerDelegate(TestSignal2.T, (_, _) => { }),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, message: null),
        };

        Assert.That(
            services,
            Has.Exactly(expectedCount: 2)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(SignalHandlerRegistration))
        );

        var handlerRegistrations = services
            .Select(d => d.ImplementationInstance)
            .OfType<SignalHandlerRegistration>()
            .Select(r => (r.SignalType, r.HandlerType, r.HandlerFn is not null))
            .ToList();

        var isDelegate =
            string.Equals(registrationMethod, "delegate", StringComparison.Ordinal)
            || string.Equals(registrationMethod, "sync_delegate", StringComparison.Ordinal);

        var expectedRegistrations = new[]
        {
            (typeof(TestSignal), isDelegate ? null : typeof(TestSignalHandler), isDelegate),
            (typeof(TestSignal2), isDelegate ? null : typeof(TestSignal2Handler), isDelegate),
        };

        Assert.That(handlerRegistrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenServiceCollection_WhenAddingSignalHandlerForMultipleSignalTypes_AddsCorrectHandlerRegistrations(
        [Values("type", "factory", "instance")] string registrationMethod
    )
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddSignalHandler<MultiTestSignalHandler>(),
            "factory" => services.AddSignalHandler(_ => new MultiTestSignalHandler()),
            "instance" => services.AddSignalHandler(new MultiTestSignalHandler()),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, message: null),
        };

        Assert.That(
            services,
            Has.Exactly(expectedCount: 2)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(SignalHandlerRegistration))
        );

        var handlerRegistrations = services
            .Select(d => d.ImplementationInstance)
            .OfType<SignalHandlerRegistration>()
            .Select(r => (r.SignalType, r.HandlerType))
            .ToList();

        var expectedRegistrations = new[]
        {
            (typeof(TestSignal), typeof(MultiTestSignalHandler)),
            (typeof(TestSignal2), typeof(MultiTestSignalHandler)),
        };

        Assert.That(handlerRegistrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandler_WhenRegisteringSameHandlerDifferently_OverwritesRegistration(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance")] string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance")] string overwrittenRegistrationMethod
    )
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestSignalHandler> factory = _ => new();
        var instance = new TestSignalHandler();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddSignalHandler<TestSignalHandler>(),
                (null, "factory") => services.AddSignalHandler(factory),
                (var l, "type") => services.AddSignalHandler<TestSignalHandler>(l.Value),
                (var l, "factory") => services.AddSignalHandler(factory, l.Value),
                (_, "instance") => services.AddSignalHandler(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, message: null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(TestSignalHandler))
        );

        // assert that we do not explicitly register handlers on their interface
        Assert.That(
            services,
            Has.Exactly(expectedCount: 0)
                .Matches<ServiceDescriptor>(d =>
                    d.ServiceType == typeof(ISignalHandler<TestSignal, TestSignal.IHandler>)
                )
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(SignalHandlerRegistration))
        );

        var handlerServiceDescriptor = services.Single(s => s.ServiceType == typeof(TestSignalHandler));
        var handlerRegistration = services
            .Select(d => d.ImplementationInstance)
            .OfType<SignalHandlerRegistration>()
            .Single();

        Assert.That(handlerRegistration.SignalType, Is.EqualTo(typeof(TestSignal)));
        Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(TestSignalHandler)));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(handlerServiceDescriptor.Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(handlerServiceDescriptor.ImplementationType, Is.EqualTo(typeof(TestSignalHandler)));

                break;

            case (var l, "factory"):
                Assert.That(handlerServiceDescriptor.Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(handlerServiceDescriptor.ImplementationFactory, Is.SameAs(factory));

                break;

            case (_, "instance"):
                Assert.That(handlerServiceDescriptor.ImplementationInstance, Is.SameAs(instance));

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(initialRegistrationMethod),
                    initialRegistrationMethod,
                    message: null
                );
        }
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandler_WhenRegisteringDifferentHandlerForSameSignalType_AddsSeparateRegistration(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime? firstLifetime,
        [Values("type", "factory", "instance", "delegate", "sync_delegate")] string firstRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime? secondLifetime,
        [Values("type", "factory", "instance", "delegate", "sync_delegate")] string secondRegistrationMethod
    )
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestSignalHandler> factory = _ => new();
        Func<IServiceProvider, DuplicateTestSignalHandler> duplicateFactory = _ => new();
        var instance = new TestSignalHandler();
        var duplicateInstance = new DuplicateTestSignalHandler();

        _ = (firstLifetime, firstRegistrationMethod) switch
        {
            (null, "type") => services.AddSignalHandler<TestSignalHandler>(),
            (null, "factory") => services.AddSignalHandler(factory),
            (var l, "type") => services.AddSignalHandler<TestSignalHandler>(l.Value),
            (var l, "factory") => services.AddSignalHandler(factory, l.Value),
            (_, "instance") => services.AddSignalHandler(instance),
            (_, "delegate") => services.AddSignalHandlerDelegate(TestSignal.T, (_, _, _) => Task.CompletedTask),
            (_, "sync_delegate") => services.AddSignalHandlerDelegate(TestSignal.T, (_, _) => { }),
            _ => throw new ArgumentOutOfRangeException(
                nameof(firstRegistrationMethod),
                firstRegistrationMethod,
                message: null
            ),
        };

        _ = (secondLifetime, secondRegistrationMethod) switch
        {
            (null, "type") => services.AddSignalHandler<DuplicateTestSignalHandler>(),
            (null, "factory") => services.AddSignalHandler(duplicateFactory),
            (var l, "type") => services.AddSignalHandler<DuplicateTestSignalHandler>(l.Value),
            (var l, "factory") => services.AddSignalHandler(duplicateFactory, l.Value),
            (_, "instance") => services.AddSignalHandler(duplicateInstance),
            (_, "delegate") => services.AddSignalHandlerDelegate(TestSignal.T, (_, _, _) => Task.CompletedTask),
            (_, "sync_delegate") => services.AddSignalHandlerDelegate(TestSignal.T, (_, _) => { }),
            _ => throw new ArgumentOutOfRangeException(
                nameof(secondRegistrationMethod),
                secondRegistrationMethod,
                message: null
            ),
        };

        Assert.That(
            services,
            Has.Exactly(expectedCount: 2)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(SignalHandlerRegistration))
        );

        var handlerRegistration1 = services
            .Select(d => d.ImplementationInstance)
            .OfType<SignalHandlerRegistration>()
            .First();
        var handlerRegistration2 = services
            .Select(d => d.ImplementationInstance)
            .OfType<SignalHandlerRegistration>()
            .Last();

        Assert.That(handlerRegistration1.SignalType, Is.EqualTo(typeof(TestSignal)));
        Assert.That(handlerRegistration2.SignalType, Is.EqualTo(typeof(TestSignal)));

        var expectedFirstLifetime = string.Equals(firstRegistrationMethod, "instance", StringComparison.Ordinal)
            ? ServiceLifetime.Singleton
            : firstLifetime ?? ServiceLifetime.Transient;
        Assert.That(
            services,
            Has.Exactly(firstRegistrationMethod is not "delegate" and not "sync_delegate" ? 1 : 0)
                .Matches<ServiceDescriptor>(d =>
                    d.ServiceType == typeof(TestSignalHandler) && d.Lifetime == expectedFirstLifetime
                )
        );

        var expectedSecondLifetime = string.Equals(secondRegistrationMethod, "instance", StringComparison.Ordinal)
            ? ServiceLifetime.Singleton
            : secondLifetime ?? ServiceLifetime.Transient;
        Assert.That(
            services,
            Has.Exactly(secondRegistrationMethod is not "delegate" and not "sync_delegate" ? 1 : 0)
                .Matches<ServiceDescriptor>(d =>
                    d.ServiceType == typeof(DuplicateTestSignalHandler) && d.Lifetime == expectedSecondLifetime
                )
        );

        switch (firstRegistrationMethod)
        {
            case "type":
            case "factory":
            case "instance":
                Assert.That(handlerRegistration1.HandlerType, Is.EqualTo(typeof(TestSignalHandler)));
                Assert.That(handlerRegistration1.HandlerFn, Is.Null);

                break;

            case "delegate":
            case "sync_delegate":
                Assert.That(handlerRegistration1.HandlerType, Is.Null);
                Assert.That(handlerRegistration1.HandlerFn, Is.Not.Null);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(secondRegistrationMethod),
                    secondRegistrationMethod,
                    message: null
                );
        }

        switch (secondRegistrationMethod)
        {
            case "type":
            case "factory":
            case "instance":
                Assert.That(handlerRegistration2.HandlerType, Is.EqualTo(typeof(DuplicateTestSignalHandler)));
                Assert.That(handlerRegistration2.HandlerFn, Is.Null);

                break;

            case "delegate":
            case "sync_delegate":
                Assert.That(handlerRegistration2.HandlerType, Is.Null);
                Assert.That(handlerRegistration2.HandlerFn, Is.Not.Null);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(secondRegistrationMethod),
                    secondRegistrationMethod,
                    message: null
                );
        }

        using var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>().For(TestSignal.T);

        Assert.That(() => handler.Handle(new(), CancellationToken.None), Throws.Nothing);
    }

    [Test]
    public void GivenServiceCollection_WhenAddingInvalidHandlerType_ThrowsInvalidOperationException()
    {
        Assert.That(
            () => new ServiceCollection().AddSignalHandler<ITestSignalHandler>(),
            Throws.InvalidOperationException.With.Message.Match("must not be an interface or abstract class")
        );

        Assert.That(
            () => new ServiceCollection().AddSignalHandler<AbstractTestSignalHandler>(),
            Throws.InvalidOperationException.With.Message.Match("must not be an interface or abstract class")
        );
    }

    [Signal]
    private sealed partial record TestSignal;

    [Signal]
    private sealed partial record TestSignal2;

    private sealed partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal signal, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed partial class TestSignal2Handler : TestSignal2.IHandler
    {
        public Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed partial class DuplicateTestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal signal, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed partial class MultiTestSignalHandler : TestSignal.IHandler, TestSignal2.IHandler
    {
        public Task Handle(TestSignal signal, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private abstract partial class AbstractTestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal signal, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1201:Elements should appear in the correct order",
        Justification = "ordering makes sense for this test"
    )]
    private interface ITestSignalHandler : TestSignal.IHandler, ISignalHandlerWithSourceGeneration;
}
