namespace Conqueror.CQS.Tests
{
    public sealed class QueryMiddlewareLifetimeTests
    {
        [Test]
        public async Task GivenTransientMiddleware_ResolvingHandlerCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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
        public async Task GivenScopedMiddleware_ResolvingHandlerCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddScoped<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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
        public async Task GivenSingletonMiddleware_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddSingleton<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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
        public async Task GivenMultipleTransientMiddlewares_ResolvingHandlerCreatesNewInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares2>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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
        public async Task GivenMultipleScopedMiddlewares_ResolvingHandlerCreatesNewInstancesForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares2>()
                        .AddScoped<TestQueryMiddleware>()
                        .AddScoped<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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
        public async Task GivenMultipleSingletonMiddlewares_ResolvingHandlerReturnsSameInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares2>()
                        .AddSingleton<TestQueryMiddleware>()
                        .AddSingleton<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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
        public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingHandlerReturnsInstancesAccordingToEachLifetime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares2>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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
                        .AddTransient<TestQueryHandlerWithRetryMiddleware>()
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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
                        .AddTransient<TestQueryHandlerWithRetryMiddleware>()
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddScoped<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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
                        .AddTransient<TestQueryHandlerWithRetryMiddleware>()
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddSingleton<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
        }

        [Test]
        public async Task GivenTransientHandlerWithRetryMiddleware_EachRetryGetsNewHandlerInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithRetryMiddleware>()
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.HandlerInvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedHandlerWithRetryMiddleware_EachRetryGetsHandlerInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddScoped<TestQueryHandlerWithRetryMiddleware>()
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.HandlerInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1, 2 }));
        }

        [Test]
        public async Task GivenSingletonHandlerWithRetryMiddleware_EachRetryGetsSameInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestQueryHandlerWithRetryMiddleware>()
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.HandlerInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
        }
        
        [Test]
        public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsNewInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithSameMiddlewareMultipleTimes>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1 }));
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private sealed record TestQuery2;

        private sealed record TestQueryResponse2;

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return new();
            }

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.Use<TestQueryMiddleware>();
        }

        private sealed class TestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>, IConfigureQueryPipeline
        {
            public async Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return new();
            }

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.Use<TestQueryMiddleware>();
        }

        private sealed class TestQueryHandlerWithMultipleMiddlewares : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return new();
            }

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestQueryMiddleware>()
                            .Use<TestQueryMiddleware2>();
            }
        }

        private sealed class TestQueryHandlerWithMultipleMiddlewares2 : IQueryHandler<TestQuery2, TestQueryResponse2>, IConfigureQueryPipeline
        {
            public async Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return new();
            }

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestQueryMiddleware>()
                            .Use<TestQueryMiddleware2>();
            }
        }

        private sealed class TestQueryHandlerWithSameMiddlewareMultipleTimes : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return new();
            }

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.UseAllowMultiple<TestQueryMiddleware>().UseAllowMultiple<TestQueryMiddleware>();
        }

        private sealed class TestQueryHandlerWithRetryMiddleware : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryHandlerWithRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.HandlerInvocationCounts.Add(invocationCount);
                return new();
            }

            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestQueryRetryMiddleware>()
                            .Use<TestQueryMiddleware>()
                            .Use<TestQueryMiddleware2>();
            }
        }

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

                _ = await ctx.Next(ctx.Query, ctx.CancellationToken);
                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestObservations
        {
            public List<int> HandlerInvocationCounts { get; } = new();
            
            public List<int> InvocationCounts { get; } = new();
        }
    }
}
