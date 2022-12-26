using System.Net;
using System.Net.Mime;
using System.Reflection;
using Conqueror.Common;
using Conqueror.CQS.Transport.Http.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
    public sealed class ConquerorContextQueryTests : TestBase
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

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenContextItems_ItemsAreReturnedInHeader(string path)
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            var response = await HttpClient.GetAsync(path);
            await response.AssertSuccessStatusCode();

            var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

            Assert.IsTrue(exists);

            var receivedItems = ContextValueFormatter.Parse(values!);

            CollectionAssert.AreEquivalent(ContextItems, receivedItems);
        }

        [Test]
        public async Task GivenContextItemsForPostQuery_ItemsAreReturnedInHeader()
        {
            Resolve<TestObservations>().ShouldAddItems = true;

            using var content = new StringContent("{\"payload\":10}", null, MediaTypeNames.Application.Json);
            var response = await HttpClient.PostAsync("/api/queries/testPost", content);
            await response.AssertSuccessStatusCode();

            var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

            Assert.IsTrue(exists);

            var receivedItems = ContextValueFormatter.Parse(values!);

            CollectionAssert.AreEquivalent(ContextItems, receivedItems);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenConquerorContextRequestHeader_ItemsAreReceivedByHandler(string path)
        {
            using var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers = { { HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(ContextItems) } },
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertSuccessStatusCode();

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [Test]
        public async Task GivenConquerorContextRequestHeaderForPostQuery_ItemsAreReceivedByHandler()
        {
            using var content = new StringContent("{\"payload\":10}", null, MediaTypeNames.Application.Json)
            {
                Headers = { { HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(ContextItems) } },
            };

            var response = await HttpClient.PostAsync("/api/queries/testPost", content);
            await response.AssertSuccessStatusCode();

            var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

            CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
        }

        [TestCase("/api/queries/test?payload=10")]
        [TestCase("/api/queries/testQueryWithoutPayload")]
        [TestCase("/api/custom/queries/test?payload=10")]
        [TestCase("/api/custom/queries/testQueryWithoutPayload")]
        public async Task GivenInvalidConquerorContextRequestHeader_ReturnsBadRequest(string path)
        {
            using var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new(path, UriKind.Relative),
                Headers = { { HttpConstants.ConquerorContextHeaderName, "foo=bar" } },
            };

            var response = await HttpClient.SendAsync(msg);
            await response.AssertStatusCode(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GivenInvalidConquerorContextRequestHeaderForPostQuery_ReturnsBadRequest()
        {
            using var content = new StringContent("{\"payload\":10}", null, MediaTypeNames.Application.Json)
            {
                Headers = { { HttpConstants.ConquerorContextHeaderName, "foo=bar" } },
            };

            var response = await HttpClient.PostAsync("/api/queries/testPost", content);
            await response.AssertStatusCode(HttpStatusCode.BadRequest);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            var applicationPartManager = new ApplicationPartManager();
            applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
            applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

            _ = services.AddSingleton(applicationPartManager);

            _ = services.AddMvc().AddConquerorCQSHttpControllers();

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandlerWithoutPayload>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddSingleton<TestObservations>();

            _ = services.AddConquerorCQS().FinalizeConquerorRegistrations();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

        [HttpQuery]
        public sealed record TestQuery;

        public sealed record TestQueryResponse;
    
        [HttpQuery]
        public sealed record TestQueryWithoutPayload;

        [HttpQuery(UsePost = true)]
        public sealed record TestPostQuery;

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

            public Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestQueryResponse());
            }
        }

        public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly TestObservations testObservations;

            public TestQueryHandlerWithoutPayload(IConquerorContextAccessor conquerorContextAccessor, TestObservations testObservations)
            {
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.testObservations = testObservations;
            }

            public Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
            {
                testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

                if (testObservations.ShouldAddItems)
                {
                    conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
                }

                return Task.FromResult(new TestQueryResponse());
            }
        }

        public sealed class TestObservations
        {
            public bool ShouldAddItems { get; set; }

            public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();
        }

        [ApiController]
        private sealed class TestHttpQueryController : ControllerBase
        {
            [HttpGet("/api/custom/queries/test")]
            public Task<TestQueryResponse> ExecuteTestQuery([FromQuery] TestQuery query, CancellationToken cancellationToken)
            {
                return HttpQueryExecutor.ExecuteQuery<TestQuery, TestQueryResponse>(HttpContext, query, cancellationToken);
            }

            [HttpGet("/api/custom/queries/testQueryWithoutPayload")]
            public Task<TestQueryResponse> ExecuteTestQueryWithoutPayload(CancellationToken cancellationToken)
            {
                return HttpQueryExecutor.ExecuteQuery<TestQueryWithoutPayload, TestQueryResponse>(HttpContext, cancellationToken);
            }
        }

        private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
        {
            public override string Name => nameof(TestControllerApplicationPart);

            public IEnumerable<TypeInfo> Types { get; } = new[] { typeof(TestHttpQueryController).GetTypeInfo() };
        }

        private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
        {
            protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpQueryController);
        }
    }
}
