using Conqueror.Signalling;

namespace Conqueror.Tests.Signalling;

[TestFixture]
public partial class SignalHandlerAssemblyScanningRegistrationTests
{
    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromExecutingAssembly_AddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
    {
        var services1 = new ServiceCollection().AddSignalHandlersFromAssembly(typeof(SignalHandlerAssemblyScanningRegistrationTests).Assembly);
        var services2 = new ServiceCollection().AddSignalHandlersFromExecutingAssembly();

        Assert.That(services2, Has.Count.EqualTo(services1.Count));
        Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
    }

    [Test]
    [TestCase(typeof(TestSignalHandler), typeof(TestSignal))]
    [TestCase(typeof(InternalTestSignalHandler), typeof(InternalTestSignal))]
    [TestCase(typeof(InternalTopLevelTestSignalHandler), typeof(InternalTopLevelTestSignal))]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_AddsSignalHandlerAsTransient(Type handlerType, Type signalType)
    {
        var services = new ServiceCollection().AddSignalHandlersFromAssembly(typeof(SignalHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == handlerType
                                                                             && d.Lifetime == ServiceLifetime.Transient));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is SignalHandlerRegistration r
                                                                             && r.SignalType == signalType
                                                                             && r.HandlerType == handlerType));
    }

    [Test]
    [TestCase(typeof(TestSignalHandler), new[] { typeof(TestSignal) })]
    [TestCase(typeof(MultiTestSignalHandler), new[] { typeof(TestSignal), typeof(TestSignal2) })]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssemblyMultipleTimes_AddsSignalHandlerAsTransientOnce(Type handlerType, Type[] signalTypes)
    {
        var services = new ServiceCollection().AddSignalHandlersFromAssembly(typeof(SignalHandlerAssemblyScanningRegistrationTests).Assembly)
                                              .AddSignalHandlersFromAssembly(typeof(SignalHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == handlerType
                                                                             && d.Lifetime == ServiceLifetime.Transient));

        Assert.That(services, Has.Exactly(signalTypes.Length).Matches<ServiceDescriptor>(d => d.ImplementationInstance is SignalHandlerRegistration r
                                                                                              && signalTypes.Any(t => t == r.SignalType)
                                                                                              && r.HandlerType == handlerType));
    }

    [Test]
    public void GivenServiceCollectionWithHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_DoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddSignalHandler<TestSignalHandler>(ServiceLifetime.Singleton)
                                              .AddSignalHandlersFromAssembly(typeof(SignalHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == typeof(TestSignalHandler)));

        Assert.That(services.Single(d => d.ServiceType == typeof(TestSignalHandler)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is SignalHandlerRegistration r
                                                                             && r.SignalType == typeof(TestSignal)
                                                                             && r.HandlerType == typeof(TestSignalHandler)));
    }

    [Test]
    public void GivenServiceCollectionWithDelegateHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_AddsOtherHandlers()
    {
        var services = new ServiceCollection().AddSignalHandlerDelegate(TestSignal.T, (_, _, _) => { })
                                              .AddSignalHandlersFromAssembly(typeof(SignalHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is SignalHandlerRegistration r
                                                                             && r.SignalType == typeof(TestSignal)
                                                                             && r.HandlerFn is not null));

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationInstance is SignalHandlerRegistration r
                                                                       && r.SignalType == typeof(TestSignal)
                                                                       && r.HandlerFn is null));
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInterfaces()
    {
        var services = new ServiceCollection().AddSignalHandlersFromAssembly(typeof(SignalHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ServiceType == typeof(ISignalHandler<TestSignal, TestSignal.IHandler>)), Is.Zero);
        Assert.That(services.Count(d => d.ServiceType == typeof(TestSignal.IHandler)), Is.Zero);
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInapplicableClasses()
    {
        var services = new ServiceCollection().AddSignalHandlersFromAssembly(typeof(SignalHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestSignalHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestSignalHandler<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestSignalHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ProtectedTestSignalHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ExplicitTestSignalHandler)));
    }

    [Signal]
    public sealed partial record TestSignal;

    [Signal]
    public sealed partial record ExplicitTestSignal;

    [Signal]
    public sealed partial record TestSignal2;

    [Signal]
    internal sealed partial record InternalTestSignal;

    [Signal]
    private sealed partial record PrivateTestSignal;

    [Signal]
    protected sealed partial record ProtectedTestSignal;

    public sealed partial class TestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed class ExplicitTestSignalHandler : ISignalHandler<ExplicitTestSignal, ExplicitTestSignalHandler>
    {
        public Task Handle(ExplicitTestSignal signal, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public abstract partial class AbstractTestSignalHandler : TestSignal.IHandler
    {
        public Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed partial class MultiTestSignalHandler : TestSignal.IHandler, TestSignal2.IHandler
    {
        public Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed class GenericTestSignalHandler<TM> : ISignalHandler<TM, GenericTestSignalHandler<TM>>
        where TM : class, ISignal<TM>
    {
        public Task Handle(TM signal, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    protected sealed partial class ProtectedTestSignalHandler : ProtectedTestSignal.IHandler
    {
        public Task Handle(ProtectedTestSignal signal, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    internal sealed partial class InternalTestSignalHandler : InternalTestSignal.IHandler
    {
        public Task Handle(InternalTestSignal signal, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed partial class PrivateTestSignalHandler : PrivateTestSignal.IHandler
    {
        public Task Handle(PrivateTestSignal signal, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

[Signal]
internal sealed partial record InternalTopLevelTestSignal;

internal sealed partial class InternalTopLevelTestSignalHandler : InternalTopLevelTestSignal.IHandler
{
    public Task Handle(InternalTopLevelTestSignal signal, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
