namespace Conqueror.Tests.Iterating;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable MA0080 // Use an overload with a CancellationToken
#pragma warning disable MA0169 // Use Equals method instead of == or != operator
#pragma warning disable RCS1174 // Remove redundant async/await

[TestFixture]
public sealed partial class IteratorHandlerRegistrationTests
{
    [Test]
    public void GivenServiceCollection_WhenRegisteringMultipleHandlers_DoesNotRegisterConquerorTypesMultipleTimes()
    {
        var services = new ServiceCollection()
            .AddIteratorHandler<TestIteratorHandler>()
            .AddIteratorHandler<TestIterator2Handler>();

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IIteratorDispatcher))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IIterators))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IInProcessIteratorClientFactory))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IIteratorIdFactory))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(Conqueror.Iterating.IteratorHandlerRegistry))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IIteratorHandlerRegistry))
        );
        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IConquerorContextAccessor))
        );
    }

    [Test]
    [Combinatorial]
    public void GivenServiceCollection_WhenAddingIteratorHandlers_AddsCorrectHandlerRegistrations(
        [Values("type", "factory", "instance", "delegate")]
        string registrationMethod
    )
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddIteratorHandler<TestIteratorHandler>().AddIteratorHandler<TestIterator2Handler>(),
            "factory" => services
                .AddIteratorHandler(_ => new TestIteratorHandler())
                .AddIteratorHandler(_ => new TestIterator2Handler()),
            "instance" => services
                .AddIteratorHandler(new TestIteratorHandler())
                .AddIteratorHandler(new TestIterator2Handler()),
            "delegate" => services
                .AddIteratorHandlerDelegate(TestIterator.T, EmptyStringIteratorHandlerFn)
                .AddIteratorHandlerDelegate(TestIterator2.T, EmptyIntIteratorHandlerFn),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, message: null),
        };

        Assert.That(
            services,
            Has.Exactly(expectedCount: 2)
                .Matches<ServiceDescriptor>(d =>
                    d.ServiceType == typeof(Conqueror.Iterating.IteratorHandlerRegistration)
                )
        );

        var handlerRegistrations = services
            .Select(d => d.ImplementationInstance)
            .OfType<Conqueror.Iterating.IteratorHandlerRegistration>()
            .Select(r => (r.IteratorType, r.ItemType, r.HandlerType, r.HandlerFn is not null))
            .ToList();

        var isDelegate = string.Equals(registrationMethod, "delegate", StringComparison.Ordinal);

        var expectedRegistrations = new[]
        {
            (typeof(TestIterator), typeof(string), isDelegate ? null : typeof(TestIteratorHandler), isDelegate),
            (typeof(TestIterator2), typeof(int), isDelegate ? null : typeof(TestIterator2Handler), isDelegate),
        };

        Assert.That(handlerRegistrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenServiceCollection_WhenAddingIteratorHandlerForMultipleIteratorTypes_AddsCorrectHandlerRegistrations(
        [Values("type", "factory", "instance")]
        string registrationMethod
    )
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddIteratorHandler<MultiTestIteratorHandler>(),
            "factory" => services.AddIteratorHandler(_ => new MultiTestIteratorHandler()),
            "instance" => services.AddIteratorHandler(new MultiTestIteratorHandler()),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, message: null),
        };

        Assert.That(
            services,
            Has.Exactly(expectedCount: 2)
                .Matches<ServiceDescriptor>(d =>
                    d.ServiceType == typeof(Conqueror.Iterating.IteratorHandlerRegistration)
                )
        );

        var handlerRegistrations = services
            .Select(d => d.ImplementationInstance)
            .OfType<Conqueror.Iterating.IteratorHandlerRegistration>()
            .Select(r => (r.IteratorType, r.HandlerType))
            .ToList();

        var expectedRegistrations = new[]
        {
            (typeof(TestIterator), typeof(MultiTestIteratorHandler)),
            (typeof(TestIterator2), typeof(MultiTestIteratorHandler)),
        };

        Assert.That(handlerRegistrations, Is.EquivalentTo(expectedRegistrations));
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
        string overwrittenRegistrationMethod
    )
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestIteratorHandler> factory = _ => new();
        var instance = new TestIteratorHandler();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddIteratorHandler<TestIteratorHandler>(),
                (null, "factory") => services.AddIteratorHandler(factory),
                (var l, "type") => services.AddIteratorHandler<TestIteratorHandler>(l.Value),
                (var l, "factory") => services.AddIteratorHandler(factory, l.Value),
                (_, "instance") => services.AddIteratorHandler(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, message: null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(TestIteratorHandler))
        );

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ServiceType == typeof(Conqueror.Iterating.IteratorHandlerRegistration)
                )
        );

        var handlerServiceDescriptor = services.Single(s => s.ServiceType == typeof(TestIteratorHandler));
        var handlerRegistration = services
            .Select(d => d.ImplementationInstance)
            .OfType<Conqueror.Iterating.IteratorHandlerRegistration>()
            .Single();

        Assert.That(handlerRegistration.IteratorType, Is.EqualTo(typeof(TestIterator)));
        Assert.That(handlerRegistration.ItemType, Is.EqualTo(typeof(string)));
        Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(TestIteratorHandler)));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(handlerServiceDescriptor.Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(handlerServiceDescriptor.ImplementationType, Is.EqualTo(typeof(TestIteratorHandler)));

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
    public void GivenRegisteredHandler_WhenRegisteringDifferentHandlerForSameIteratorType_OverwritesRegistration(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string overwrittenRegistrationMethod
    )
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestIteratorHandler> factory = _ => new();
        Func<IServiceProvider, DuplicateTestIteratorHandler> duplicateFactory = _ => new();
        var instance = new TestIteratorHandler();
        var duplicateInstance = new DuplicateTestIteratorHandler();

        _ = (initialLifetime, initialRegistrationMethod) switch
        {
            (null, "type") => services.AddIteratorHandler<TestIteratorHandler>(),
            (null, "factory") => services.AddIteratorHandler(factory),
            (var l, "type") => services.AddIteratorHandler<TestIteratorHandler>(l.Value),
            (var l, "factory") => services.AddIteratorHandler(factory, l.Value),
            (_, "instance") => services.AddIteratorHandler(instance),
            (_, "delegate") => services.AddIteratorHandlerDelegate(TestIterator.T, ThrowingStringIteratorHandlerFn),
            _ => throw new ArgumentOutOfRangeException(
                nameof(initialRegistrationMethod),
                initialRegistrationMethod,
                message: null
            ),
        };

        _ = (overwrittenLifetime, overwrittenRegistrationMethod) switch
        {
            (null, "type") => services.AddIteratorHandler<DuplicateTestIteratorHandler>(),
            (null, "factory") => services.AddIteratorHandler(duplicateFactory),
            (var l, "type") => services.AddIteratorHandler<DuplicateTestIteratorHandler>(l.Value),
            (var l, "factory") => services.AddIteratorHandler(duplicateFactory, l.Value),
            (_, "instance") => services.AddIteratorHandler(duplicateInstance),
            (_, "delegate") => services.AddIteratorHandlerDelegate(TestIterator.T, SuccessfulStringIteratorHandlerFn),
            _ => throw new ArgumentOutOfRangeException(
                nameof(overwrittenRegistrationMethod),
                overwrittenRegistrationMethod,
                message: null
            ),
        };

        Assert.That(
            services,
            Has.Exactly(expectedCount: 1)
                .Matches<ServiceDescriptor>(d =>
                    d.ServiceType == typeof(Conqueror.Iterating.IteratorHandlerRegistration)
                )
        );

        var handlerRegistration = services
            .Select(d => d.ImplementationInstance)
            .OfType<Conqueror.Iterating.IteratorHandlerRegistration>()
            .Single();

        Assert.That(handlerRegistration.IteratorType, Is.EqualTo(typeof(TestIterator)));
        Assert.That(handlerRegistration.ItemType, Is.EqualTo(typeof(string)));

        var expectedInitialLifetime = string.Equals(initialRegistrationMethod, "instance", StringComparison.Ordinal)
            ? ServiceLifetime.Singleton
            : initialLifetime ?? ServiceLifetime.Transient;
        Assert.That(
            services,
            Has.Exactly(initialRegistrationMethod is not "delegate" ? 1 : 0)
                .Matches<ServiceDescriptor>(d =>
                    d.ServiceType == typeof(TestIteratorHandler) && d.Lifetime == expectedInitialLifetime
                )
        );

        var expectedOverwrittenLifetime = string.Equals(
            overwrittenRegistrationMethod,
            "instance",
            StringComparison.Ordinal
        )
            ? ServiceLifetime.Singleton
            : overwrittenLifetime ?? ServiceLifetime.Transient;
        Assert.That(
            services,
            Has.Exactly(overwrittenRegistrationMethod is not "delegate" ? 1 : 0)
                .Matches<ServiceDescriptor>(d =>
                    d.ServiceType == typeof(DuplicateTestIteratorHandler) && d.Lifetime == expectedOverwrittenLifetime
                )
        );

        switch (overwrittenRegistrationMethod)
        {
            case "type":
            case "factory":
            case "instance":
                Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(DuplicateTestIteratorHandler)));
                Assert.That(handlerRegistration.HandlerFn, Is.Null);

                break;

            case "delegate":
                Assert.That(handlerRegistration.HandlerType, Is.Null);
                Assert.That(handlerRegistration.HandlerFn, Is.Not.Null);

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(overwrittenRegistrationMethod),
                    overwrittenRegistrationMethod,
                    message: null
                );
        }

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        // this asserts that the overwriting handler is called since the original handler would throw
        Assert.That(async () => await ConsumeAll(client.Handle(new(), CancellationToken.None)), Throws.Nothing);
    }

    [Test]
    public void GivenServiceCollection_WhenAddingInvalidHandlerType_ThrowsInvalidOperationException()
    {
        Assert.That(
            () => new ServiceCollection().AddIteratorHandler<ITestIteratorHandler>(),
            Throws.InvalidOperationException.With.Message.Match("must not be an interface or abstract class")
        );

        Assert.That(
            () => new ServiceCollection().AddIteratorHandler<AbstractTestIteratorHandler>(),
            Throws.InvalidOperationException.With.Message.Match("must not be an interface or abstract class")
        );
    }

    private static async Task<List<T>> ConsumeAll<T>(IAsyncEnumerable<T> items)
    {
        var result = new List<T>();
        await foreach (var item in items)
        {
            result.Add(item);
        }

        return result;
    }

    private static async IAsyncEnumerable<string> EmptyStringIteratorHandlerFn(
        TestIterator iterator,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        yield break;
    }

    private static async IAsyncEnumerable<int> EmptyIntIteratorHandlerFn(
        TestIterator2 iterator,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        yield break;
    }

    private static async IAsyncEnumerable<string> ThrowingStringIteratorHandlerFn(
        TestIterator iterator,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        throw new NotSupportedException();
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<string> SuccessfulStringIteratorHandlerFn(
        TestIterator iterator,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        yield return "item";
    }

    [Iterator<string>]
    private sealed partial record TestIterator;

    [Iterator<int>]
    private sealed partial record TestIterator2;

    private sealed partial class TestIteratorHandler : TestIterator.IHandler
    {
        public async IAsyncEnumerable<string> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            throw new NotSupportedException();
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }

    private sealed partial class TestIterator2Handler : TestIterator2.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator2 iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            throw new NotSupportedException();
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }

    private sealed partial class DuplicateTestIteratorHandler : TestIterator.IHandler
    {
        public async IAsyncEnumerable<string> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            yield return "item";
        }
    }

    private abstract partial class AbstractTestIteratorHandler : TestIterator.IHandler
    {
        public async IAsyncEnumerable<string> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            yield return "item";
        }
    }

    private sealed partial class MultiTestIteratorHandler : TestIterator.IHandler, TestIterator2.IHandler
    {
        public async IAsyncEnumerable<string> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            yield return "item";
        }

        public async IAsyncEnumerable<int> Handle(
            TestIterator2 iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            yield return 1;
        }
    }

    [SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1201:Elements should appear in the correct order",
        Justification = "ordering makes sense for this test"
    )]
    private interface ITestIteratorHandler : TestIterator.IHandler, IIteratorHandlerWithSourceGeneration;
}
