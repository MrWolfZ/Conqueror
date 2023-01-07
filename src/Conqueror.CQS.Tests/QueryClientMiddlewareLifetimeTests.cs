namespace Conqueror.CQS.Tests
{
    public sealed class QueryClientMiddlewareLifetimeTests
    {
        [Test]
        public async Task GivenTransientMiddleware_ResolvingClientCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedMiddleware_ResolvingClientCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddScoped<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
        }

        [Test]
        public async Task GivenSingletonMiddleware_ResolvingClientReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddSingleton<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public async Task GivenMultipleTransientMiddlewares_ResolvingClientCreatesNewInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware>().Use<TestQueryMiddleware2>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport,
                                                                                                p => p.Use<TestQueryMiddleware>().Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenMultipleScopedMiddlewares_ResolvingClientCreatesNewInstancesForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware>().Use<TestQueryMiddleware2>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport,
                                                                                                p => p.Use<TestQueryMiddleware>().Use<TestQueryMiddleware2>())
                        .AddScoped<TestQueryMiddleware>()
                        .AddScoped<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 1, 1, 2, 2 }));
        }

        [Test]
        public async Task GivenMultipleSingletonMiddlewares_ResolvingClientReturnsSameInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware>().Use<TestQueryMiddleware2>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport,
                                                                                                p => p.Use<TestQueryMiddleware>().Use<TestQueryMiddleware2>())
                        .AddSingleton<TestQueryMiddleware>()
                        .AddSingleton<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 }));
        }

        [Test]
        public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingClientReturnsInstancesAccordingToEachLifetime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware>().Use<TestQueryMiddleware2>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport,
                                                                                                p => p.Use<TestQueryMiddleware>().Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5 }));
        }

        [Test]
        public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionGetsNewInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryRetryMiddleware>()
                                                                                                    .Use<TestQueryMiddleware>()
                                                                                                    .Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryRetryMiddleware>()
                                                                                                    .Use<TestQueryMiddleware>()
                                                                                                    .Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddScoped<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 1, 1, 2, 1 }));
        }

        [Test]
        public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryRetryMiddleware>()
                                                                                                    .Use<TestQueryMiddleware>()
                                                                                                    .Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddSingleton<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
        }

        [Test]
        public async Task GivenTransientTransportWithRetryMiddleware_EachRetryGetsNewTransportInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>(),
                                                                                              p => p.Use<TestQueryRetryMiddleware>()
                                                                                                    .Use<TestQueryMiddleware>()
                                                                                                    .Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryTransport>()
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.TransportInvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsNewInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.UseAllowMultiple<TestQueryMiddleware>().UseAllowMultiple<TestQueryMiddleware>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1 }));
        }

        [Test]
        public async Task GivenTransientMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
        }

        [Test]
        public async Task GivenScopedMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddScoped<TestQueryMiddleware>()
                        .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
        }

        [Test]
        public async Task GivenSingletonMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddConquerorQueryClient<IQueryHandler<TestQuery2, TestQueryResponse2>>(CreateTransport, p => p.Use<TestQueryMiddleware>())
                        .AddSingleton<TestQueryMiddleware>()
                        .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
        }

        private static IQueryTransportClient CreateTransport(IQueryTransportClientBuilder builder)
        {
            return new TestQueryTransport(builder.ServiceProvider.GetRequiredService<TestObservations>());
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private sealed record TestQuery2;

        private sealed record TestQueryResponse2;

        private sealed class TestQueryMiddleware : IQueryMiddleware
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestQueryMiddleware2 : IQueryMiddleware
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestQueryRetryMiddleware : IQueryMiddleware
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

                _ = await ctx.Next(ctx.Query, ctx.CancellationToken);
                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestQueryTransport : IQueryTransportClient
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryTransport(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
                where TQuery : class
            {
                await Task.Yield();

                invocationCount += 1;
                observations.TransportInvocationCounts.Add(invocationCount);

                if (typeof(TQuery) == typeof(TestQuery))
                {
                    return (TResponse)(object)new TestQueryResponse();
                }

                if (typeof(TQuery) == typeof(TestQuery2))
                {
                    return (TResponse)(object)new TestQueryResponse2();
                }

                throw new InvalidOperationException("should never reach this");
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
