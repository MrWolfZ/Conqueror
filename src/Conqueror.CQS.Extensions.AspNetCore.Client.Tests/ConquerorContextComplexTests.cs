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

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConqueror();

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestCommandHandler>()
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
                        .AddConquerorCommandHttpClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => HttpClient);

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

        public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandler(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
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
            public bool ShouldAddItems { get; set; }

            public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();
        }
    }
}
