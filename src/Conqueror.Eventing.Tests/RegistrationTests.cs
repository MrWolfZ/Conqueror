namespace Conqueror.Eventing.Tests
{
    [TestFixture]
    public sealed class RegistrationTests
    {
        [Test]
        public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
        {
            var services = new ServiceCollection().AddConquerorEventing().AddConquerorEventing();

            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IEventPublisher)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(IEventObserver<>)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(EventObserverRegistry)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(EventMiddlewaresInvoker)));
            Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(EventingRegistrationFinalizer)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromExecutingAssemblyAddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
        {
            var services1 = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);
            var services2 = new ServiceCollection().AddConquerorEventingTypesFromExecutingAssembly();

            Assert.AreEqual(services1.Count, services2.Count);
            Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithPlainInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserver) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithCustomInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithMultiplePlainInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultiplePlainInterfaces) && d.Lifetime == ServiceLifetime.Transient));
            Assert.AreEqual(
                1, services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultiplePlainInterfaces) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithMultipleCustomInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultipleCustomInterfaces) &&
                                            d.Lifetime == ServiceLifetime.Transient));
            Assert.AreEqual(
                1, services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultipleCustomInterfaces) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithMultipleMixedInterfaceAsTransient()
        {
            var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultipleMixedInterfaces) && d.Lifetime == ServiceLifetime.Transient));
            Assert.AreEqual(
                1, services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultipleMixedInterfaces) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverMiddlewareAsTransient()
        {
            var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverMiddleware) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverMiddlewareWithoutConfigurationAsTransient()
        {
            var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(
                services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverMiddlewareWithoutConfiguration) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventPublisherMiddlewareAsTransient()
        {
            var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsTrue(services.Any(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventPublisherMiddleware) && d.Lifetime == ServiceLifetime.Transient));
        }

        [Test]
        public void GivenServiceCollectionWithObserverAlreadyRegistered_AddingAllTypesFromAssemblyDoesNotAddObserverAgain()
        {
            var services = new ServiceCollection().AddSingleton<TestEventObserver>().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.AreEqual(1, services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserver)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddInterfaces()
        {
            var services = new ServiceCollection().AddSingleton<TestEventObserver>().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(ITestEventObserver)));
        }

        [Test]
        public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddAbstractClasses()
        {
            var services = new ServiceCollection().AddSingleton<TestEventObserver>().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

            Assert.IsFalse(services.Any(d => d.ServiceType == typeof(AbstractTestEventObserver)));
        }

        [Test]
        public void GivenServiceCollectionWithConquerorEventingRegistrationWithoutFinalization_ThrowsExceptionWhenBuildingServiceProviderWithValidation()
        {
            var services = new ServiceCollection().AddConquerorEventing();

            var ex = Assert.Throws<AggregateException>(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true }));

            Assert.IsInstanceOf<InvalidOperationException>(ex?.InnerException);
            Assert.That(ex?.InnerException?.Message, Contains.Substring("DidYouForgetToCallFinalizeConquerorRegistrations"));
        }

        [Test]
        public void GivenServiceCollectionWithConquerorEventingRegistrationWithFinalization_ThrowsExceptionWhenCallingFinalizationAgain()
        {
            var services = new ServiceCollection().AddConquerorEventing().FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
        }

        [Test]
        public void GivenServiceCollectionWithFinalization_ThrowsExceptionWhenRegisteringEventing()
        {
            var services = new ServiceCollection().FinalizeConquerorRegistrations();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorEventing());
        }

        private sealed record TestEvent;

        private sealed record TestEvent2;

        private interface ITestEventObserver : IEventObserver<TestEvent>
        {
        }

        private interface ITestEventObserver2 : IEventObserver<TestEvent2>
        {
        }

        private sealed class TestEventObserver : IEventObserver<TestEvent>
        {
            public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class TestEventObserverWithCustomInterface : ITestEventObserver
        {
            public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class TestEventObserverWithMultiplePlainInterfaces : IEventObserver<TestEvent>, IEventObserver<TestEvent2>
        {
            public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class TestEventObserverWithMultipleCustomInterfaces : ITestEventObserver, ITestEventObserver2
        {
            public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class TestEventObserverWithMultipleMixedInterfaces : ITestEventObserver, IEventObserver<TestEvent2>
        {
            public Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private abstract class AbstractTestEventObserver : IEventObserver<TestEvent>
        {
            public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class TestEventObserverMiddlewareConfiguration
        {
        }

        private sealed class TestEventObserverMiddleware : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>
        {
            public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
                where TEvent : class =>
                ctx.Next(ctx.Event, ctx.CancellationToken);
        }

        private sealed class TestEventObserverMiddlewareWithoutConfiguration : IEventObserverMiddleware
        {
            public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
                where TEvent : class =>
                ctx.Next(ctx.Event, ctx.CancellationToken);
        }

        private sealed class TestEventPublisherMiddleware : IEventPublisherMiddleware
        {
            public Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
                where TEvent : class =>
                ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }
}
