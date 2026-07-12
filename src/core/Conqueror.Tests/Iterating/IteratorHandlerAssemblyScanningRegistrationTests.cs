namespace Conqueror.Tests.Iterating;

using System.Runtime.CompilerServices;
using static InternalHandlerContainer;

[TestFixture]
public partial class IteratorHandlerAssemblyScanningRegistrationTests
{
    [Test]
    [TestCase(typeof(TestIteratorHandler), typeof(TestIterator), typeof(TestItem))]
    [TestCase(typeof(InternalTestIteratorHandler), typeof(InternalTestIterator), typeof(TestItem))]
    [TestCase(typeof(InternalTopLevelTestIteratorHandler), typeof(InternalTopLevelTestIterator), typeof(TestItem))]
    [TestCase(typeof(InternalNestedTestIteratorHandler), typeof(InternalNestedTestIterator), typeof(TestItem))]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_AddsIteratorHandlerAsTransient(
        Type handlerType,
        Type iteratorType,
        Type itemType
    )
    {
        var services = new ServiceCollection().AddIteratorHandlersFromAssembly(
            typeof(IteratorHandlerAssemblyScanningRegistrationTests).Assembly
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationType == d.ServiceType
                    && d.ServiceType == handlerType
                    && d.Lifetime is ServiceLifetime.Transient
                )
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationInstance is Conqueror.Iterating.IteratorHandlerRegistration r
                    && r.IteratorType == iteratorType
                    && r.ItemType == itemType
                    && r.HandlerType == handlerType
                )
        );
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_AddsIteratorHandlerWithMultipleIteratorTypesAsTransient()
    {
        var services = new ServiceCollection().AddIteratorHandlersFromAssembly(
            typeof(IteratorHandlerAssemblyScanningRegistrationTests).Assembly
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationType == d.ServiceType
                    && d.ServiceType == typeof(MultiTestIteratorHandler)
                    && d.Lifetime is ServiceLifetime.Transient
                )
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 2)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationInstance is Conqueror.Iterating.IteratorHandlerRegistration r
                    && r.HandlerType == typeof(MultiTestIteratorHandler)
                )
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationInstance is Conqueror.Iterating.IteratorHandlerRegistration r
                    && r.IteratorType == typeof(TestIteratorForMulti1)
                    && r.ItemType == typeof(TestItem)
                    && r.HandlerType == typeof(MultiTestIteratorHandler)
                )
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationInstance is Conqueror.Iterating.IteratorHandlerRegistration r
                    && r.IteratorType == typeof(TestIteratorForMulti2)
                    && r.ItemType == typeof(TestItem)
                    && r.HandlerType == typeof(MultiTestIteratorHandler)
                )
        );
    }

    [Test]
    [TestCase(typeof(TestIteratorHandler), typeof(TestIterator), typeof(TestItem))]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssemblyMultipleTimes_AddsIteratorHandlerAsTransientOnce(
        Type handlerType,
        Type iteratorType,
        Type itemType
    )
    {
        var services = new ServiceCollection()
            .AddIteratorHandlersFromAssembly(typeof(IteratorHandlerAssemblyScanningRegistrationTests).Assembly)
            .AddIteratorHandlersFromAssembly(typeof(IteratorHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationType == d.ServiceType
                    && d.ServiceType == handlerType
                    && d.Lifetime is ServiceLifetime.Transient
                )
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationInstance is Conqueror.Iterating.IteratorHandlerRegistration r
                    && r.IteratorType == iteratorType
                    && r.ItemType == itemType
                    && r.HandlerType == handlerType
                )
        );
    }

    [Test]
    public void GivenServiceCollectionWithHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_DoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection()
            .AddIteratorHandler<TestIteratorHandler>(ServiceLifetime.Singleton)
            .AddIteratorHandlersFromAssembly(typeof(IteratorHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestIteratorHandler)
                )
        );

        Assert.That(
            services.Single(d => d.ServiceType == typeof(TestIteratorHandler)).Lifetime,
            Is.EqualTo(ServiceLifetime.Singleton)
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationInstance is Conqueror.Iterating.IteratorHandlerRegistration r
                    && r.IteratorType == typeof(TestIterator)
                    && r.ItemType == typeof(TestItem)
                    && r.HandlerType == typeof(TestIteratorHandler)
                )
        );
    }

    [Test]
    public void GivenServiceCollectionWithDelegateHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_DoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection()
            .AddIteratorHandlerDelegate(TestIterator.T, TestIteratorHandlerFn)
            .AddIteratorHandlersFromAssembly(typeof(IteratorHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ImplementationInstance is Conqueror.Iterating.IteratorHandlerRegistration r
                    && r.IteratorType == typeof(TestIterator)
                    && r.ItemType == typeof(TestItem)
                )
        );
    }

    private static async IAsyncEnumerable<TestItem> TestIteratorHandlerFn(
        TestIterator iterator,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        yield return new TestItem();
        await Task.CompletedTask;
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInterfaces()
    {
        var services = new ServiceCollection().AddIteratorHandlersFromAssembly(
            typeof(IteratorHandlerAssemblyScanningRegistrationTests).Assembly
        );

        Assert.That(
            services.Count(d =>
                d.ServiceType
                == typeof(IIteratorHandler<
                    TestIterator,
                    TestItem,
                    TestIterator.IHandler,
                    TestIterator.IHandler.Proxy,
                    TestIterator.IPipeline,
                    TestIterator.IPipeline.Proxy
                >)
            ),
            Is.Zero
        );
        Assert.That(services.Count(d => d.ServiceType == typeof(TestIterator.IHandler)), Is.Zero);
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInapplicableClasses()
    {
        var services = new ServiceCollection().AddIteratorHandlersFromAssembly(
            typeof(IteratorHandlerAssemblyScanningRegistrationTests).Assembly
        );

        Assert.That(
            services,
            Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestIteratorHandler))
        );
        Assert.That(
            services,
            Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestIteratorHandler))
        );
        Assert.That(
            services,
            Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ProtectedTestIteratorHandler))
        );
        Assert.That(
            services,
            Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ExplicitTestIteratorHandler))
        );
    }

    [Iterator<TestItem>]
    public sealed partial record TestIterator;

    public sealed record TestItem;

    [Iterator<ExplicitTestItem>]
    public sealed partial record ExplicitTestIterator;

    public sealed record ExplicitTestItem;

    [Iterator<TestItem2>]
    public sealed partial record TestIterator2;

    public sealed record TestItem2;

    [Iterator<TestItem>]
    public sealed partial record TestIteratorForMulti1;

    [Iterator<TestItem>]
    public sealed partial record TestIteratorForMulti2;

    [Iterator<TestItem>]
    internal sealed partial record InternalTestIterator;

    [Iterator<TestItem>]
    protected sealed partial record ProtectedTestIterator;

    [Iterator<TestItem>]
    private sealed partial record PrivateTestIterator;

    public sealed partial class TestIteratorHandler : TestIterator.IHandler
    {
        public async IAsyncEnumerable<TestItem> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            yield return new TestItem();
            await Task.CompletedTask;
        }
    }

    public sealed class ExplicitTestIteratorHandler
        : IIteratorHandler<
            ExplicitTestIterator,
            ExplicitTestItem,
            ExplicitTestIterator.IHandler,
            ExplicitTestIterator.IHandler.Proxy,
            ExplicitTestIterator.IPipeline,
            ExplicitTestIterator.IPipeline.Proxy
        >
    {
        static IEnumerable<IIteratorHandlerTypesInjector> IIteratorHandler.GetTypeInjectors() => [];

        public async IAsyncEnumerable<ExplicitTestItem> Handle(
            ExplicitTestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            _ = iterator;
            _ = cancellationToken;
            yield return new ExplicitTestItem();
            await Task.CompletedTask;
        }

        public static IAsyncEnumerable<ExplicitTestItem> Invoke(
            ExplicitTestIterator.IHandler handler,
            ExplicitTestIterator iterator,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }

    public abstract partial class AbstractTestIteratorHandler : TestIterator.IHandler
    {
        public async IAsyncEnumerable<TestItem> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            yield return new TestItem();
            await Task.CompletedTask;
        }
    }

    public sealed partial class MultiTestIteratorHandler
        : TestIteratorForMulti1.IHandler,
          TestIteratorForMulti2.IHandler
    {
        public async IAsyncEnumerable<TestItem> Handle(
            TestIteratorForMulti1 iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            yield return new TestItem();
            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<TestItem> Handle(
            TestIteratorForMulti2 iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            yield return new TestItem();
            await Task.CompletedTask;
        }
    }

    internal sealed partial class InternalTestIteratorHandler : InternalTestIterator.IHandler
    {
        public async IAsyncEnumerable<TestItem> Handle(
            InternalTestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            yield return new TestItem();
            await Task.CompletedTask;
        }
    }

    protected sealed partial class ProtectedTestIteratorHandler : ProtectedTestIterator.IHandler
    {
        public async IAsyncEnumerable<TestItem> Handle(
            ProtectedTestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            yield return new TestItem();
            await Task.CompletedTask;
        }
    }

    private sealed partial class PrivateTestIteratorHandler : PrivateTestIterator.IHandler
    {
        public async IAsyncEnumerable<TestItem> Handle(
            PrivateTestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            yield return new TestItem();
            await Task.CompletedTask;
        }
    }
}

[Iterator<IteratorHandlerAssemblyScanningRegistrationTests.TestItem>]
internal sealed partial record InternalTopLevelTestIterator;

internal sealed partial class InternalTopLevelTestIteratorHandler : InternalTopLevelTestIterator.IHandler
{
    public async IAsyncEnumerable<IteratorHandlerAssemblyScanningRegistrationTests.TestItem> Handle(
        InternalTopLevelTestIterator iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        yield return new IteratorHandlerAssemblyScanningRegistrationTests.TestItem();
        await Task.CompletedTask;
    }
}

internal sealed partial class InternalHandlerContainer
{
    [Iterator<IteratorHandlerAssemblyScanningRegistrationTests.TestItem>]
    internal sealed partial record InternalNestedTestIterator;

    internal sealed partial class InternalNestedTestIteratorHandler : InternalNestedTestIterator.IHandler
    {
        public async IAsyncEnumerable<IteratorHandlerAssemblyScanningRegistrationTests.TestItem> Handle(
            InternalNestedTestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            yield return new IteratorHandlerAssemblyScanningRegistrationTests.TestItem();
            await Task.CompletedTask;
        }
    }
}
