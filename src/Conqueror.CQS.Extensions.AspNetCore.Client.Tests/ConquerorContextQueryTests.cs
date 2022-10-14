using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public class ConquerorContextQueryTests : TestBase
    {
        private static readonly Dictionary<string, string> ContextItems = new()
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "keyWith,Comma", "value" },
            { "key4", "valueWith,Comma" },
            { "keyWith=Equals", "value" },
            { "key6", "valueWith=Equals" },
        };

        [Test]
        public async Task GivenManuallyCreatedContextOnClientAndContextItemsInHandler_ItemsAreReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

            var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ContextItems, context.Items);
        }

        [Test]
        public async Task GivenManuallyCreatedContextOnClientAndContextItemsInPostHandler_ItemsAreReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

            var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ContextItems, context.Items);
        }

        [Test]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInHandler()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.AddOrReplaceItems(ContextItems);

            var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInHandlerAcrossMultipleInvocations()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.Items.Add(ContextItems.First());

            var observations = Resolve<TestObservations>();

            observations.ShouldAddItems = true;

            var allReceivedKeys = new List<string>();

            var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
        }

        [Test]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInPostHandler()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.AddOrReplaceItems(ContextItems);

            var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInPostHandlerAcrossMultipleInvocations()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.Items.Add(ContextItems.First());

            var observations = Resolve<TestObservations>();

            observations.ShouldAddItems = true;

            var allReceivedKeys = new List<string>();

            var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
        }

        [Test]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInDifferentHandlersAcrossMultipleInvocations()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.Items.Add(ContextItems.First());

            var observations = Resolve<TestObservations>();

            observations.ShouldAddItems = true;

            var allReceivedKeys = new List<string>();

            var handler1 = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();
            var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler1.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
        }

        [Test]
        public async Task GivenContextItemsInHandler_ContextIsReceivedInOuterHandler()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenContextItemsInPostHandler_ContextIsReceivedInOuterHandler()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            var handler = ResolveOnClient<IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenContextItemsInOuterHandler_ContextIsReceivedInHandler()
        {
            ResolveOnClient<TestObservations>().ShouldAddOuterItems = true;

            var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenContextItemsInOuterHandler_ContextIsReceivedInPostHandler()
        {
            ResolveOnClient<TestObservations>().ShouldAddOuterItems = true;

            var handler = ResolveOnClient<IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(), CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConqueror();

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddSingleton<TestObservations>();

            _ = services.AddConquerorCQS().ConfigureConqueror();
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorCqsHttpClientServices(o =>
            {
                o.HttpClientFactory = uri =>
                    throw new InvalidOperationException(
                        $"during tests all clients should be explicitly configured with the test http client; got request to create http client for base address '{uri}'");

                o.JsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                };
            });

            _ = services.AddConquerorQueryHttpClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => HttpClient)
                        .AddConquerorQueryHttpClient<IQueryHandler<TestPostQuery, TestQueryResponse>>(_ => HttpClient);

            _ = services.AddTransient<OuterTestQueryHandler>()
                        .AddTransient<OuterTestPostQueryHandler>()
                        .AddSingleton<TestObservations>();

            _ = services.AddConquerorCQS().ConfigureConqueror();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

        public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestQueryHandler(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestQueryResponse());
            }
        }

        public sealed class TestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestPostQueryHandler(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken)
            {
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestQueryResponse());
            }
        }

        public sealed record OuterTestQuery;

        public sealed record OuterTestQueryResponse;

        public sealed class OuterTestQueryHandler : IQueryHandler<OuterTestQuery, OuterTestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly IQueryHandler<TestQuery, TestQueryResponse> nestedHandler;
            private readonly TestObservations testObservations;

            public OuterTestQueryHandler(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations, IQueryHandler<TestQuery, TestQueryResponse> nestedHandler)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
                this.nestedHandler = nestedHandler;
            }

            public async Task<OuterTestQueryResponse> ExecuteQuery(OuterTestQuery query, CancellationToken cancellationToken)
            {
                if (testObservations.ShouldAddOuterItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                _ = await nestedHandler.ExecuteQuery(new(), cancellationToken);
                testObservations.ReceivedOuterContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);
                return new();
            }
        }

        public sealed record OuterTestPostQuery;

        public sealed class OuterTestPostQueryHandler : IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly IQueryHandler<TestPostQuery, TestQueryResponse> nestedHandler;
            private readonly TestObservations testObservations;

            public OuterTestPostQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                             TestObservations testObservations,
                                             IQueryHandler<TestPostQuery, TestQueryResponse> nestedHandler)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
                this.nestedHandler = nestedHandler;
            }

            public async Task<OuterTestQueryResponse> ExecuteQuery(OuterTestPostQuery query, CancellationToken cancellationToken)
            {
                if (testObservations.ShouldAddOuterItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                _ = await nestedHandler.ExecuteQuery(new(), cancellationToken);
                testObservations.ReceivedOuterContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);
                return new();
            }
        }

        public sealed class TestObservations
        {
            public bool ShouldAddItems { get; set; }

            public bool ShouldAddOuterItems { get; set; }

            public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();

            public IDictionary<string, string> ReceivedOuterContextItems { get; } = new Dictionary<string, string>();
        }
    }
}
