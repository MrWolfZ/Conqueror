#if NET6_0_OR_GREATER

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
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
            var result = await response.Content.ReadFromJsonAsync<TopLevelProgram.TopLevelTestQueryResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task GivenTopLevelProgramWithCommandHandler_CallingCommandHandlerViaHttpWorks()
        {
            var response = await HttpClient.PostAsJsonAsync("/api/commands/topLevelTest", new TopLevelProgram.TopLevelTestCommand(10));
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TopLevelProgram.TopLevelTestCommandResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
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
