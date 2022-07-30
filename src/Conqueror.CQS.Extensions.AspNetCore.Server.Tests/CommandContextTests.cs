using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public sealed class CommandContextTests : TestBase
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
        public async Task GivenNonTransferrableCommandContextItems_ItemsAreNotReturnedInHeader(string path, string data)
        {
            Resolve<TestObservations>().ShouldAddItems = true;
            
            using var content = new StringContent(data, null, MediaTypeNames.Application.Json);
            var response = await HttpClient.PostAsync(path, content);
            await response.AssertSuccessStatusCode();

            var exists = response.Headers.TryGetValues(HttpConstants.CommandContextHeaderName, out var values);

            Assert.IsFalse(exists);

            var receivedItems = ContextValueFormatter.Parse(values ?? new List<string>());

            Assert.That(receivedItems, Is.Empty);
        }

        [TestCase("/api/commands/test", "{}")]
        [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
        [TestCase("/api/commands/testCommandWithoutPayload", "")]
        [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
        public async Task GivenTransferrableCommandContextItems_ItemsAreReturnedInHeader(string path, string data)
        {
            Resolve<TestObservations>().ShouldAddTransferrableItems = true;

            using var content = new StringContent(data, null, MediaTypeNames.Application.Json);
            var response = await HttpClient.PostAsync(path, content);
            await response.AssertSuccessStatusCode();

            var exists = response.Headers.TryGetValues(HttpConstants.CommandContextHeaderName, out var values);

            Assert.IsTrue(exists);

            var receivedItems = ContextValueFormatter.Parse(values!);

            CollectionAssert.AreEquivalent(ContextItems, receivedItems);
        }

        [TestCase("/api/commands/test", "{}")]
        [TestCase("/api/commands/testCommandWithoutResponse", "{}")]
        [TestCase("/api/commands/testCommandWithoutPayload", "")]
        [TestCase("/api/commands/testCommandWithoutResponseWithoutPayload", "")]
        public async Task GivenCommandContextRequestHeader_ItemsAreReceivedByHandler(string path, string data)
        {
            using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
            {
                Headers = { { HttpConstants.CommandContextHeaderName, ContextValueFormatter.Format(ContextItems) } },
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
        public async Task GivenInvalidCommandContextRequestHeader_ReturnsBadRequest(string path, string data)
        {
            using var content = new StringContent(data, null, MediaTypeNames.Application.Json)
            {
                Headers = { { HttpConstants.CommandContextHeaderName, "foo=bar" } },
            };

            var response = await HttpClient.PostAsync(path, content);
            await response.AssertStatusCode(HttpStatusCode.BadRequest);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConqueror();

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
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandler(ICommandContextAccessor commandContextAccessor, TestObservations testObservations)
            {
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                testObservations.AddItems(commandContextAccessor.CommandContext!.TransferrableItems);

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
                testObservations.AddItems(commandContextAccessor.CommandContext!.TransferrableItems);

                if (testObservations.ShouldAddItems)
                {
                    commandContextAccessor.CommandContext?.AddItems(ContextItems.ToDictionary(p => (object)p.Key, p => (object?)p.Value));
                }

                if (testObservations.ShouldAddTransferrableItems)
                {
                    commandContextAccessor.CommandContext?.AddTransferrableItems(ContextItems);
                }
                
                return Task.CompletedTask;
            }
        }

        public sealed class TestCommandHandlerWithoutPayload : ICommandHandler<TestCommandWithoutPayload, TestCommandResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandlerWithoutPayload(ICommandContextAccessor commandContextAccessor, TestObservations testObservations)
            {
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestCommandResponse> ExecuteCommand(TestCommandWithoutPayload command, CancellationToken cancellationToken)
            {
                testObservations.AddItems(commandContextAccessor.CommandContext!.TransferrableItems);

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

        public sealed class TestCommandHandlerWithoutResponseWithoutPayload : ICommandHandler<TestCommandWithoutResponseWithoutPayload>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly TestObservations testObservations;

            public TestCommandHandlerWithoutResponseWithoutPayload(ICommandContextAccessor commandContextAccessor, TestObservations testObservations)
            {
                this.commandContextAccessor = commandContextAccessor;
                this.testObservations = testObservations;
            }

            public Task ExecuteCommand(TestCommandWithoutResponseWithoutPayload command, CancellationToken cancellationToken)
            {
                testObservations.AddItems(commandContextAccessor.CommandContext!.TransferrableItems);

                if (testObservations.ShouldAddItems)
                {
                    commandContextAccessor.CommandContext?.AddItems(ContextItems.ToDictionary(p => (object)p.Key, p => (object?)p.Value));
                }

                if (testObservations.ShouldAddTransferrableItems)
                {
                    commandContextAccessor.CommandContext?.AddTransferrableItems(ContextItems);
                }
                
                return Task.CompletedTask;
            }
        }

        public sealed class TestObservations
        {
            public bool ShouldAddItems { get; set; }

            public bool ShouldAddTransferrableItems { get; set; }

            public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();

            public void AddItems(IEnumerable<KeyValuePair<string, string>> source)
            {
                foreach (var p in source)
                {
                    ReceivedContextItems[p.Key] = p.Value;
                }
            }
        }
    }
}
