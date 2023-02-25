using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [TestFixture]
    [NonParallelizable]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public class ConquerorContextComplexTests : TestBase
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
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
        public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInDifferentCommandAndQueryHandlersAcrossMultipleInvocations()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
            context.Items.Add(ContextItems.First());

            var observations = Resolve<TestObservations>();

            observations.ShouldAddItems = true;

            var allReceivedKeys = new List<string>();

            var handler1 = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
            observations.ReceivedContextItems.Clear();

            Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
        }

        [Test]
        public async Task GivenManuallyCreatedContextOnClient_SameTraceIdIsReceivedInDifferentCommandAndQueryHandlersAcrossMultipleInvocations()
        {
            using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

            var observations = Resolve<TestObservations>();

            var handler1 = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

            _ = await handler1.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(new[] { context.TraceId, context.TraceId, context.TraceId }, observations.ReceivedTraceIds);
        }

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers();

            _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                        .AddConquerorCommandHandler<TestCommandHandler>()
                        .AddSingleton<TestObservations>();
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorCQSHttpClientServices(o =>
            {
                _ = o.UseHttpClient(HttpClient);

                o.JsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                };
            });

            var baseAddress = new Uri("http://localhost");

            _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.UseHttp(baseAddress))
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => b.UseHttp(baseAddress));
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

        [HttpQuery]
        public sealed record TestQuery
        {
            public int Payload { get; init; }
        }

        public sealed record TestQueryResponse
        {
            public int Payload { get; init; }
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

            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestQueryResponse());
            }
        }

        [HttpCommand]
        public sealed record TestCommand
        {
            public int Payload { get; init; }
        }

        public sealed record TestCommandResponse
        {
            public int Payload { get; init; }
        }

        public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandler(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestCommandResponse());
            }
        }

        public sealed class TestObservations
        {
            public List<string?> ReceivedTraceIds { get; } = new();

            public bool ShouldAddItems { get; set; }

            public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();
        }
    }
}
