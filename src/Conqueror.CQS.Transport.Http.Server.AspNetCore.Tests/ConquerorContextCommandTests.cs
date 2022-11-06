using System.Net;
using System.Net.Mime;
using Conqueror.Common;
using Conqueror.CQS.Transport.Http.Common;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public sealed class ConquerorContextCommandTests : TestBase
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

        [TestCase("/api/commands/test", "{}")]
        [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
        [TestCase("/api/commands/testCommandWithoutPayload", "")]
        [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
        public async Task GivenContextItems_ItemsAreReturnedInHeader(string path, string data)
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            using var content = new StringContent(data, null, MediaTypeNames.Application.Json);
            var response = await HttpClient.PostAsync(path, content);
            await response.AssertSuccessStatusCode();

            var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

            Assert.IsTrue(exists);

            var receivedItems = ContextValueFormatter.Parse(values!);

            CollectionAssert.AreEquivalent(ContextItems, receivedItems);
        }

        [TestCase("/api/commands/test", "{}")]
        [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
        [TestCase("/api/commands/testCommandWithoutPayload", "")]
        [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
        public async Task GivenConquerorContextRequestHeader_ItemsAreReceivedByHandler(string path, string data)
        {
            using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
            {
                Headers = { { HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(ContextItems) } },
            };

            var response = await HttpClient.PostAsync(path, content);
            await response.AssertSuccessStatusCode();

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [TestCase("/api/commands/test", "{}")]
        [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
        [TestCase("/api/commands/testCommandWithoutPayload", "")]
        [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
        public async Task GivenInvalidConquerorContextRequestHeader_ReturnsBadRequest(string path, string data)
        {
            using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
            {
                Headers = { { HttpConstants.ConquerorContextHeaderName, "foo=bar" } },
            };

            var response = await HttpClient.PostAsync(path, content);
            await response.AssertStatusCode(HttpStatusCode.BadRequest);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQS();

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddTransient<TestCommandHandlerWithoutPayload>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutPayload>()
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

        public sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandlerWithoutResponse(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
            }

            public Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }
                
                return Task.CompletedTask;
            }
        }

        public sealed class TestCommandHandlerWithoutPayload : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandlerWithoutPayload(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken)
            {
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }
                
                return Task.FromResult(new TestCommandResponse());
            }
        }

        public sealed class TestCommandHandlerWithoutResponseWithoutPayload : ICommandHandler<TestCommandWithoutResponseWithoutPayload>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandlerWithoutResponseWithoutPayload(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
            }

            public Task ExecuteCommand(TestCommandWithoutResponseWithoutPayload command, CancellationToken cancellationToken)
            {
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }
                
                return Task.CompletedTask;
            }
        }

        public sealed class TestObservations
        {
            public bool ShouldAddItems { get; set; }

            public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();
        }
    }
}
