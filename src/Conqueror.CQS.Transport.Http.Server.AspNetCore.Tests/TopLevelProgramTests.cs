#if NET6_0_OR_GREATER

using System.Net;
using System.Net.Http.Json;
using Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests.TopLevelProgram;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in TearDown")]
    public class TopLevelProgramTests
    {
        [SetUp]
        public void SetUp()
        {
            applicationFactory = new();

            client = applicationFactory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            applicationFactory?.Dispose();
            client?.Dispose();
        }

        [Test]
        public async Task GivenTopLevelProgramWithQueryHandler_CallingQueryHandlerViaHttpWorks()
        {
            var response = await HttpClient.GetAsync("/api/queries/topLevelTest?payload=10");
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TopLevelTestQueryResponse>();

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        [Test]
        public async Task GivenTopLevelProgramWithCommandHandler_CallingCommandHandlerViaHttpWorks()
        {
            var response = await HttpClient.PostAsJsonAsync("/api/commands/topLevelTest", new TopLevelTestCommand(10));
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TopLevelTestCommandResponse>();

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Payload, Is.EqualTo(11));
        }

        private WebApplicationFactory<Program>? applicationFactory;
        private HttpClient? client;

        private HttpClient HttpClient
        {
            get
            {
                if (client == null)
                {
                    throw new InvalidOperationException("test fixture must be initialized before using http client");
                }

                return client;
            }
        }
    }
}

#endif
