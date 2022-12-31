namespace Conqueror.CQS.Tests
{
    public sealed class QueryClientMiddlewareFunctionalityTests
    {
        [Test]
        public async Task GivenClientWithNoMiddleware_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport)
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.Empty);
            Assert.That(observations.MiddlewareTypes, Is.Empty);
        }

        [Test]
        public async Task GivenClientWithSingleAppliedMiddleware_MiddlewareIsCalledWithQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()))
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware) }));
        }

        [Test]
        public async Task GivenClientWithSingleAppliedMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new() { Parameter = 10 }))
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(10), CancellationToken.None);

            Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestQueryMiddlewareConfiguration { Parameter = 10 } }));
        }

        [Test]
        public async Task GivenClientWithMultipleAppliedMiddlewares_MiddlewaresAreCalledWithQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware), typeof(TestQueryMiddleware2) }));
        }

        [Test]
        public async Task GivenClientWithSameMiddlewareAppliedMultipleTimes_MiddlewareIsCalledWithQueryMultipleTimes()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.UseAllowMultiple<TestQueryMiddleware2>()
                                                                                                    .UseAllowMultiple<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware2) }));
        }

        [Test]
        public async Task GivenClientWithAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.UseAllowMultiple<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .Use<TestQueryMiddleware2>()
                                                                                                    .UseAllowMultiple<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .Without<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware), typeof(TestQueryMiddleware) }));
        }

        [Test]
        public async Task GivenClientWithAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.UseAllowMultiple<TestQueryMiddleware2>()
                                                                                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .UseAllowMultiple<TestQueryMiddleware2>()
                                                                                                    .Without<TestQueryMiddleware, TestQueryMiddlewareConfiguration>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware2) }));
        }

        [Test]
        public async Task GivenClientWithMultipleAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.UseAllowMultiple<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .UseAllowMultiple<TestQueryMiddleware2>()
                                                                                                    .UseAllowMultiple<TestQueryMiddleware2>()
                                                                                                    .UseAllowMultiple<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .Without<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware), typeof(TestQueryMiddleware) }));
        }

        [Test]
        public async Task GivenClientWithMultipleAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.UseAllowMultiple<TestQueryMiddleware2>()
                                                                                                    .UseAllowMultiple<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .UseAllowMultiple<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .UseAllowMultiple<TestQueryMiddleware2>()
                                                                                                    .Without<TestQueryMiddleware, TestQueryMiddlewareConfiguration>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware2) }));
        }

        [Test]
        public void GivenPipelineWithExistingMiddleware_WhenAddingSameMiddlewareAgain_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware2>()
                                                                                                    .Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(new(10), CancellationToken.None));
        }

        [Test]
        public void GivenPipelineWithExistingMiddlewareWithConfiguration_WhenAddingSameMiddlewareAgain_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()))
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(new(10), CancellationToken.None));
        }

        [Test]
        public void GivenPipelineWithExistingMiddleware_WhenAddingSameMiddlewareAgainAsAllowingMultiple_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware2>()
                                                                                                    .UseAllowMultiple<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(new(10), CancellationToken.None));
        }

        [Test]
        public void GivenPipelineWithExistingMiddlewareWithConfiguration_WhenAddingSameMiddlewareAgainAsAllowingMultiple_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .UseAllowMultiple<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()))
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(new(10), CancellationToken.None));
        }

        [Test]
        public void GivenPipelineWithExistingMiddleware_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_DoesNotThrowInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware2>()
                                                                                                    .Without<TestQueryMiddleware2>()
                                                                                                    .Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            Assert.DoesNotThrowAsync(() => handler.ExecuteQuery(new(10), CancellationToken.None));
        }

        [Test]
        public void GivenPipelineWithExistingMiddlewareWithConfiguration_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_DoesNotThrowInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .Without<TestQueryMiddleware, TestQueryMiddlewareConfiguration>()
                                                                                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()))
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            Assert.DoesNotThrowAsync(() => handler.ExecuteQuery(new(10), CancellationToken.None));
        }

        [Test]
        public async Task GivenClientWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesWithQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryRetryMiddleware>()
                                                                                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query, query, query, query }));
            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestQueryRetryMiddleware), typeof(TestQueryMiddleware), typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware), typeof(TestQueryMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                                                                                    .Use<TestQueryMiddleware2>())
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            using var tokenSource = new CancellationTokenSource();

            _ = await handler.ExecuteQuery(new(10), tokenSource.Token);

            Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
        }

        [Test]
        public async Task GivenMiddlewares_MiddlewaresCanChangeTheQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<MutatingTestQueryMiddleware>()
                                                                                                    .Use<MutatingTestQueryMiddleware2>())
                        .AddTransient<MutatingTestQueryMiddleware>()
                        .AddTransient<MutatingTestQueryMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(0), CancellationToken.None);

            var query1 = new TestQuery(0);
            var query2 = new TestQuery(1);
            var query3 = new TestQuery(3);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query1, query2 }));
            Assert.That(observations.QueriesFromTransports, Is.EquivalentTo(new[] { query3 }));
        }

        [Test]
        public async Task GivenMiddlewares_MiddlewaresCanChangeTheResponse()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<MutatingTestQueryMiddleware>()
                                                                                                    .Use<MutatingTestQueryMiddleware2>())
                        .AddTransient<MutatingTestQueryMiddleware>()
                        .AddTransient<MutatingTestQueryMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response = await handler.ExecuteQuery(new(0), CancellationToken.None);

            var response1 = new TestQueryResponse(4);
            var response2 = new TestQueryResponse(5);
            var response3 = new TestQueryResponse(7);

            Assert.That(observations.ResponsesFromMiddlewares, Is.EquivalentTo(new[] { response1, response2 }));
            Assert.AreEqual(response3, response);
        }

        [Test]
        public async Task GivenMiddlewares_MiddlewaresCanChangeTheCancellationTokens()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false) } };

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<MutatingTestQueryMiddleware>()
                                                                                                    .Use<MutatingTestQueryMiddleware2>())
                        .AddTransient<MutatingTestQueryMiddleware>()
                        .AddTransient<MutatingTestQueryMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(0), tokens.CancellationTokens[0]);

            Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
            Assert.That(observations.CancellationTokensFromTransports, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
        }

        [Test]
        public async Task GivenPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var observedInstances = new List<TestService>();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => observedInstances.Add(p.ServiceProvider.GetRequiredService<TestService>()))
                        .AddScoped<TestService>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(10), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(10), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(10), CancellationToken.None);

            Assert.That(observedInstances, Has.Count.EqualTo(3));
            Assert.AreNotSame(observedInstances[0], observedInstances[1]);
            Assert.AreSame(observedInstances[0], observedInstances[2]);
        }

        [Test]
        public void GivenPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware2>())
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(new(10), CancellationToken.None));

            Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
            Assert.That(exception?.Message, Contains.Substring(nameof(TestQueryMiddleware2)));
        }

        [Test]
        public void GivenPipelineThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()))
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(new(10), CancellationToken.None));

            Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
            Assert.That(exception?.Message, Contains.Substring(nameof(TestQueryMiddleware)));
        }

        [Test]
        public void GivenMiddlewareThatThrows_InvocationThrowsSameException()
        {
            var services = new ServiceCollection();
            var exception = new Exception();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(CreateTransport,
                                                                                              p => p.Use<ThrowingTestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()))
                        .AddTransient<ThrowingTestQueryMiddleware>()
                        .AddSingleton(exception);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteQuery(new(10), CancellationToken.None));

            Assert.AreSame(exception, thrownException);
        }

        private static IQueryTransportClient CreateTransport(IQueryTransportClientBuilder builder)
        {
            return new TestQueryTransport(builder.ServiceProvider.GetRequiredService<TestObservations>());
        }

        private sealed record TestQuery(int Payload);

        private sealed record TestQueryResponse(int Payload);

        private sealed record TestQueryMiddlewareConfiguration
        {
            public int Parameter { get; set; }
        }

        private sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            private readonly TestObservations observations;

            public TestQueryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.ConfigurationFromMiddlewares.Add(ctx.Configuration);

                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestQueryMiddleware2 : IQueryMiddleware
        {
            private readonly TestObservations observations;

            public TestQueryMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestQueryRetryMiddleware : IQueryMiddleware
        {
            private readonly TestObservations observations;

            public TestQueryRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                _ = await ctx.Next(ctx.Query, ctx.CancellationToken);
                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class MutatingTestQueryMiddleware : IQueryMiddleware
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestQueryMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                var query = ctx.Query;

                if (query is TestQuery testQuery)
                {
                    query = (TQuery)(object)(testQuery with { Payload = testQuery.Payload + 1 });
                }

                var response = await ctx.Next(query, cancellationTokensToUse.CancellationTokens[1]);

                observations.ResponsesFromMiddlewares.Add(response!);

                if (response is TestQueryResponse testQueryResponse)
                {
                    response = (TResponse)(object)(testQueryResponse with { Payload = testQueryResponse.Payload + 2 });
                }

                return response;
            }
        }

        private sealed class MutatingTestQueryMiddleware2 : IQueryMiddleware
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestQueryMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                var query = ctx.Query;

                if (query is TestQuery testQuery)
                {
                    query = (TQuery)(object)(testQuery with { Payload = testQuery.Payload + 2 });
                }

                var response = await ctx.Next(query, cancellationTokensToUse.CancellationTokens[2]);

                observations.ResponsesFromMiddlewares.Add(response!);

                if (response is TestQueryResponse testQueryResponse)
                {
                    response = (TResponse)(object)(testQueryResponse with { Payload = testQueryResponse.Payload + 1 });
                }

                return response;
            }
        }

        private sealed class ThrowingTestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            private readonly Exception exception;

            public ThrowingTestQueryMiddleware(Exception exception)
            {
                this.exception = exception;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class
            {
                await Task.Yield();
                throw exception;
            }
        }

        private sealed class TestQueryTransport : IQueryTransportClient
        {
            private readonly TestObservations observations;

            public TestQueryTransport(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
                where TQuery : class
            {
                await Task.Yield();

                observations.QueriesFromTransports.Add(query);
                observations.CancellationTokensFromTransports.Add(cancellationToken);

                return (TResponse)(object)new TestQueryResponse((query as TestQuery)!.Payload + 1);
            }
        }

        private sealed class TestObservations
        {
            public List<Type> MiddlewareTypes { get; } = new();

            public List<object> QueriesFromTransports { get; } = new();

            public List<object> QueriesFromMiddlewares { get; } = new();

            public List<object> ResponsesFromMiddlewares { get; } = new();

            public List<CancellationToken> CancellationTokensFromTransports { get; } = new();

            public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = new();

            public List<object> ConfigurationFromMiddlewares { get; } = new();
        }

        private sealed class CancellationTokensToUse
        {
            public List<CancellationToken> CancellationTokens { get; } = new();
        }

        private sealed class TestService
        {
        }
    }
}
