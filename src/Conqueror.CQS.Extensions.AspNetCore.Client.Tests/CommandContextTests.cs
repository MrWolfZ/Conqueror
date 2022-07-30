using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public class CommandContextTests : TestBase
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
        public async Task GivenClientContextCaptureEnabledAndTransferrableCommandContextItemsInHandler_ItemsAreReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddTransferrableItems = true;

            var clientContext = ResolveOnClient<ICommandClientContext>();

            clientContext.CaptureResponseItems();

            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ContextItems, clientContext.ResponseItems);
        }

        [Test]
        public async Task GivenClientContextCaptureEnabledAndTransferrableCommandContextItemsInHandlerWithoutResponse_ItemsAreReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddTransferrableItems = true;

            var clientContext = ResolveOnClient<ICommandClientContext>();

            clientContext.CaptureResponseItems();

            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            CollectionAssert.AreEquivalent(ContextItems, clientContext.ResponseItems);
        }

        [Test]
        public async Task GivenClientContextCaptureEnabledAndNonTransferrableCommandContextItemsInHandler_ItemsAreNotReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            var clientContext = ResolveOnClient<ICommandClientContext>();

            clientContext.CaptureResponseItems();

            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            Assert.That(clientContext.ResponseItems, Is.Empty);
        }

        [Test]
        public async Task GivenClientContextCaptureEnabledAndNonTransferrableCommandContextItemsInHandlerWithoutResponse_ItemsAreNotReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            var clientContext = ResolveOnClient<ICommandClientContext>();

            clientContext.CaptureResponseItems();

            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            Assert.That(clientContext.ResponseItems, Is.Empty);
        }

        [Test]
        public async Task GivenClientContextCaptureIsNotEnabledAndTransferrableCommandContextItemsInHandler_ItemsAreNotReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddTransferrableItems = true;

            var clientContext = ResolveOnClient<ICommandClientContext>();

            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            Assert.That(clientContext.ResponseItems, Is.Empty);
        }

        [Test]
        public async Task GivenClientContextCaptureIsNotEnabledAndTransferrableCommandContextItemsInHandlerWithoutResponse_ItemsAreNotReturnedInClientContext()
        {
            Resolve<TestObservations>().ShouldAddTransferrableItems = true;

            var clientContext = ResolveOnClient<ICommandClientContext>();

            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            Assert.That(clientContext.ResponseItems, Is.Empty);
        }

        [Test]
        public async Task GivenClientContextItems_ContextIsReceivedInHandler()
        {
            var clientContext = ResolveOnClient<ICommandClientContext>();
            clientContext.Items.SetRange(ContextItems);

            var handler = ResolveOnClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenClientContextItems_ContextIsReceivedInHandlerWithoutResponse()
        {
            var clientContext = ResolveOnClient<ICommandClientContext>();
            clientContext.Items.SetRange(ContextItems);

            var handler = ResolveOnClient<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new() { Payload = 10 }, CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenTransferrableContextItemsInHandler_ContextIsReceivedInOuterHandler()
        {
            Resolve<TestObservations>().ShouldAddTransferrableItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommand, OuterTestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenTransferrableContextItemsInHandlerWithoutResponse_ContextIsReceivedInOuterHandler()
        {
            Resolve<TestObservations>().ShouldAddTransferrableItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenNonTransferrableContextItemsInHandler_ContextIsNotReceivedInOuterHandler()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommand, OuterTestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

            Assert.That(receivedContextItems, Is.Empty);
        }

        [Test]
        public async Task GivenNonTransferrableContextItemsInHandlerWithoutResponse_ContextIsNotReceivedInOuterHandler()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

            Assert.That(receivedContextItems, Is.Empty);
        }

        [Test]
        public async Task GivenTransferrableContextItemsInOuterHandler_ContextIsReceivedInHandler()
        {
            ResolveOnClient<TestObservations>().ShouldAddOuterTransferrableItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommand, OuterTestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenTransferrableContextItemsInOuterHandler_ContextIsReceivedInHandlerWithoutResponse()
        {
            ResolveOnClient<TestObservations>().ShouldAddOuterTransferrableItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenNonTransferrableContextItemsInOuterHandler_ContextIsNotReceivedInHandler()
        {
            ResolveOnClient<TestObservations>().ShouldAddOuterItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommand, OuterTestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            Assert.That(receivedContextItems, Is.Empty);
        }

        [Test]
        public async Task GivenNonTransferrableContextItemsInOuterHandler_ContextIsNotReceivedInHandlerWithoutResponse()
        {
            ResolveOnClient<TestObservations>().ShouldAddOuterItems = true;

            var handler = ResolveOnClient<ICommandHandler<OuterTestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new(), CancellationToken.None);

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            Assert.That(receivedContextItems, Is.Empty);
        }

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConqueror();
            _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestCommandWithCustomSerializedPayloadTypePayloadJsonConverterFactory()); });

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddSingleton<TestObservations>();

            _ = services.AddConquerorCQS().ConfigureConqueror();
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorHttpClients()
                        .ConfigureDefaultHttpClientOptions(o =>
                        {
                            o.HttpClientFactory = _ => HttpClient;
                            o.JsonSerializerOptionsFactory = _ => new()
                            {
                                Converters = { new TestCommandWithCustomSerializedPayloadTypePayloadJsonConverterFactory() },
                                PropertyNameCaseInsensitive = true,
                            };
                        })
                        .AddCommandHttpClient<ICommandHandler<TestCommand, TestCommandResponse>>()
                        .AddCommandHttpClient<ICommandHandler<TestCommandWithoutResponse>>();

            _ = services.AddTransient<OuterTestCommandHandler>()
                        .AddTransient<OuterTestCommandWithoutResponseHandler>()
                        .AddSingleton<TestObservations>();

            _ = services.AddConquerorCQS().ConfigureConqueror();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

        public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandler(ICommandContextAccessor commandContextAccessor, TestObservations testObservations)
            {
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                testObservations.ReceivedContextItems.SetRange(commandContextAccessor.CommandContext!.TransferrableItems);

                if (testObservations.ShouldAddItems)
                {
                    commandContextAccessor.CommandContext?.AddItems(ContextItems.ToDictionary(p => (object)p.Key, p => (object?)p.Value));
                }

                if (testObservations.ShouldAddTransferrableItems)
                {
                    commandContextAccessor.CommandContext?.AddTransferrableItems(ContextItems);
                }

                return Task.FromResult(new TestCommandResponse());
            }
        }

        public sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandlerWithoutResponse(ICommandContextAccessor commandContextAccessor, TestObservations testObservations)
            {
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
            }

            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                testObservations.ReceivedContextItems.SetRange(commandContextAccessor.CommandContext!.TransferrableItems);

                if (testObservations.ShouldAddItems)
                {
                    commandContextAccessor.CommandContext?.AddItems(ContextItems.ToDictionary(p => (object)p.Key, p => (object?)p.Value));
                }

                if (testObservations.ShouldAddTransferrableItems)
                {
                    commandContextAccessor.CommandContext?.AddTransferrableItems(ContextItems);
                }

                return Task.FromResult(new TestCommandResponse());
            }
        }

        public sealed record OuterTestCommand;

        public sealed record OuterTestCommandResponse;

        public sealed class OuterTestCommandHandler : ICommandHandler<OuterTestCommand, OuterTestCommandResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly ICommandHandler<TestCommand, TestCommandResponse> nestedHandler;
            private readonly TestObservations testObservations;

            public OuterTestCommandHandler(ICommandContextAccessor commandContextAccessor, TestObservations testObservations, ICommandHandler<TestCommand, TestCommandResponse> nestedHandler)
            {
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
                this.nestedHandler = nestedHandler;
            }

            public async Task<OuterTestCommandResponse> ExecuteCommand(OuterTestCommand command, CancellationToken cancellationToken)
            {
                if (testObservations.ShouldAddOuterItems)
                {
                    commandContextAccessor.CommandContext?.AddItems(ContextItems.ToDictionary(p => (object)p.Key, p => (object?)p.Value));
                }

                if (testObservations.ShouldAddOuterTransferrableItems)
                {
                    commandContextAccessor.CommandContext?.AddTransferrableItems(ContextItems);
                }

                _ = await nestedHandler.ExecuteCommand(new(), cancellationToken);
                testObservations.ReceivedOuterContextItems.SetRange(commandContextAccessor.CommandContext!.TransferrableItems);
                return new();
            }
        }

        public sealed record OuterTestCommandWithoutResponse;

        public sealed class OuterTestCommandWithoutResponseHandler : ICommandHandler<OuterTestCommandWithoutResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly ICommandHandler<TestCommandWithoutResponse> nestedHandler;
            private readonly TestObservations testObservations;

            public OuterTestCommandWithoutResponseHandler(ICommandContextAccessor commandContextAccessor, 
                                                          TestObservations testObservations,
                                                          ICommandHandler<TestCommandWithoutResponse> nestedHandler)
            {
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
                this.nestedHandler = nestedHandler;
            }

            public async Task ExecuteCommand(OuterTestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                if (testObservations.ShouldAddOuterItems)
                {
                    commandContextAccessor.CommandContext?.AddItems(ContextItems.ToDictionary(p => (object)p.Key, p => (object?)p.Value));
                }

                if (testObservations.ShouldAddOuterTransferrableItems)
                {
                    commandContextAccessor.CommandContext?.AddTransferrableItems(ContextItems);
                }

                await nestedHandler.ExecuteCommand(new(), cancellationToken);
                testObservations.ReceivedOuterContextItems.SetRange(commandContextAccessor.CommandContext!.TransferrableItems);
            }
        }

        public sealed class TestObservations
        {
            public bool ShouldAddItems { get; set; }

            public bool ShouldAddTransferrableItems { get; set; }

            public bool ShouldAddOuterItems { get; set; }

            public bool ShouldAddOuterTransferrableItems { get; set; }

            public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();

            public IDictionary<string, string> ReceivedOuterContextItems { get; } = new Dictionary<string, string>();
        }
    }
}
