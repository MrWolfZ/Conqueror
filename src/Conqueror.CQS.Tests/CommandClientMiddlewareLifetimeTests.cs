using Conqueror.CQS.Common;

namespace Conqueror.CQS.Tests
{
    public sealed class CommandClientMiddlewareLifetimeTests
    {
        [Test]
        public async Task GivenTransientMiddleware_ResolvingClientCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedMiddleware_ResolvingClientCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddScoped<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 1, 2, 3 }));
        }

        [Test]
        public async Task GivenSingletonMiddleware_ResolvingClientReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddSingleton<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));
        }

        [Test]
        public async Task GivenMultipleTransientMiddlewares_ResolvingClientCreatesNewInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport,
                                                                                                      p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport,
                                                                                                        p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport,
                                                                                                p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenMultipleScopedMiddlewares_ResolvingClientCreatesNewInstancesForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport,
                                                                                                      p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport,
                                                                                                        p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport,
                                                                                                p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddScoped<TestCommandMiddleware>()
                        .AddScoped<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 1, 1, 2, 2, 3, 3 }));
        }

        [Test]
        public async Task GivenMultipleSingletonMiddlewares_ResolvingClientReturnsSameInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport,
                                                                                                      p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport,
                                                                                                        p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport,
                                                                                                p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddSingleton<TestCommandMiddleware>()
                        .AddSingleton<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 }));
        }

        [Test]
        public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingClientReturnsInstancesAccordingToEachLifetime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport,
                                                                                                      p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport,
                                                                                                        p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport,
                                                                                                p => p.Use<TestCommandMiddleware>().Use<TestCommandMiddleware2>())
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7 }));
        }

        [Test]
        public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionGetsNewInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport,
                                                                                                      p => p.Use<TestCommandRetryMiddleware>()
                                                                                                            .Use<TestCommandMiddleware>()
                                                                                                            .Use<TestCommandMiddleware2>())
                        .AddTransient<TestCommandRetryMiddleware>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport,
                                                                                                      p => p.Use<TestCommandRetryMiddleware>()
                                                                                                            .Use<TestCommandMiddleware>()
                                                                                                            .Use<TestCommandMiddleware2>())
                        .AddTransient<TestCommandRetryMiddleware>()
                        .AddScoped<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 1, 1, 2, 1 }));
        }

        [Test]
        public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport,
                                                                                                      p => p.Use<TestCommandRetryMiddleware>()
                                                                                                            .Use<TestCommandMiddleware>()
                                                                                                            .Use<TestCommandMiddleware2>())
                        .AddTransient<TestCommandRetryMiddleware>()
                        .AddSingleton<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
        }

        [Test]
        public async Task GivenTransientTransportWithRetryMiddleware_EachRetryGetsNewTransportInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>(),
                                                                                                      p => p.Use<TestCommandRetryMiddleware>()
                                                                                                            .Use<TestCommandMiddleware>()
                                                                                                            .Use<TestCommandMiddleware2>())
                        .AddTransient<TestCommandTransport>()
                        .AddTransient<TestCommandRetryMiddleware>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.TransportInvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsNewInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport,
                                                                                                      p => p.UseAllowMultiple<TestCommandMiddleware>().UseAllowMultiple<TestCommandMiddleware>())
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1 }));
        }

        [Test]
        public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimesForHandlerWithoutResponse_EachExecutionGetsNewInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport,
                                                                                                p => p.UseAllowMultiple<TestCommandMiddleware>().UseAllowMultiple<TestCommandMiddleware>())
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1 }));
        }

        [Test]
        public async Task GivenTransientMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddTransient<TestCommandMiddleware>()
                        .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 1, 2, 3 }));
        }

        [Test]
        public async Task GivenScopedMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddScoped<TestCommandMiddleware>()
                        .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 1, 2, 3 }));
        }

        [Test]
        public async Task GivenSingletonMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommand2, TestCommandResponse2>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(CreateTransport, p => p.Use<TestCommandMiddleware>())
                        .AddSingleton<TestCommandMiddleware>()
                        .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 1, 2, 3 }));
        }

        private static ICommandTransportClient CreateTransport(ICommandTransportClientBuilder builder)
        {
            return new TestCommandTransport(builder.ServiceProvider.GetRequiredService<TestObservations>());
        }

        private sealed record TestCommand;

        private sealed record TestCommandResponse;

        private sealed record TestCommand2;

        private sealed record TestCommandResponse2;

        private sealed record TestCommandWithoutResponse;

        private sealed class TestCommandMiddleware : ICommandMiddleware
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestCommandMiddleware2 : ICommandMiddleware
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestCommandRetryMiddleware : ICommandMiddleware
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

                _ = await ctx.Next(ctx.Command, ctx.CancellationToken);
                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestCommandTransport : ICommandTransportClient
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandTransport(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
                where TCommand : class
            {
                await Task.Yield();

                invocationCount += 1;
                observations.TransportInvocationCounts.Add(invocationCount);

                if (typeof(TCommand) == typeof(TestCommand))
                {
                    return (TResponse)(object)new TestCommandResponse();
                }

                if (typeof(TCommand) == typeof(TestCommand2))
                {
                    return (TResponse)(object)new TestCommandResponse2();
                }

                return (TResponse)(object)UnitCommandResponse.Instance;
            }
        }

        private sealed class DependencyResolvedDuringMiddlewareExecution
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public DependencyResolvedDuringMiddlewareExecution(TestObservations observations)
            {
                this.observations = observations;
            }

            public void Execute()
            {
                invocationCount += 1;
                observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts.Add(invocationCount);
            }
        }

        private sealed class TestObservations
        {
            public List<int> TransportInvocationCounts { get; } = new();

            public List<int> InvocationCounts { get; } = new();

            public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = new();
        }
    }
}
