namespace Conqueror.CQS.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for assembly scanning to work")]
    public sealed class CommandMiddlewareRegistryTests
    {
        [Test]
        public void GivenManuallyRegisteredCommandMiddleware_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandMiddleware>()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandMiddlewareRegistry>();

            var expectedRegistrations = new[]
            {
                new CommandMiddlewareRegistration(typeof(TestCommandMiddleware), typeof(TestCommandMiddlewareConfiguration)),
            };

            var registrations = registry.GetCommandMiddlewareRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenManuallyRegisteredCommandMiddlewareWithoutConfiguration_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandMiddlewareWithoutConfiguration>()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandMiddlewareRegistry>();

            var expectedRegistrations = new[]
            {
                new CommandMiddlewareRegistration(typeof(TestCommandMiddlewareWithoutConfiguration), null),
            };

            var registrations = registry.GetCommandMiddlewareRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenMultipleManuallyRegisteredCommandMiddlewares_ReturnsRegistrations()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestCommandMiddleware>()
                                                  .AddTransient<TestCommandMiddleware2>()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandMiddlewareRegistry>();

            var expectedRegistrations = new[]
            {
                new CommandMiddlewareRegistration(typeof(TestCommandMiddleware), typeof(TestCommandMiddlewareConfiguration)),
                new CommandMiddlewareRegistration(typeof(TestCommandMiddleware2), null),
            };

            var registrations = registry.GetCommandMiddlewareRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenCommandMiddlewaresRegisteredViaAssemblyScanning_ReturnsRegistrations()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddConquerorCQSTypesFromExecutingAssembly()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<ICommandMiddlewareRegistry>();

            var registrations = registry.GetCommandMiddlewareRegistrations();

            Assert.That(registrations, Contains.Item(new CommandMiddlewareRegistration(typeof(TestCommandMiddleware), typeof(TestCommandMiddlewareConfiguration))));
            Assert.That(registrations, Contains.Item(new CommandMiddlewareRegistration(typeof(TestCommandMiddleware2), null)));
        }

        public sealed class TestCommandMiddlewareConfiguration
        {
        }

        public sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }

        public sealed class TestCommandMiddlewareWithoutConfiguration : ICommandMiddleware
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }

        public sealed class TestCommandMiddleware2 : ICommandMiddleware
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class =>
                ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }
}
