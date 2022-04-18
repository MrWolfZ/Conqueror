using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
{
    [TestFixture]
    public sealed class CommandHttpEndpointTests : TestBase
    {
        [Test]
        public async Task HttpCommandTest()
        {
            var response = await HttpClient.PostAsJsonAsync("/api/commands/test", new TestCommand { Payload = 10 });
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TestCommandResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task HttpCommandWithoutResponseTest()
        {
            var response = await HttpClient.PostAsJsonAsync("/api/commands/testCommandWithoutResponse", new TestCommand { Payload = 10 });
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();

            Assert.IsEmpty(result);
        }

        [Test]
        public async Task HttpCommandWithoutPayloadTest()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TestCommandResponse>();

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result!.Payload);
        }

        [Test]
        public async Task HttpCommandWithoutResponseWithoutPayloadTest()
        {
            using var content = new StringContent(string.Empty);
            var response = await HttpClient.PostAsync("/api/commands/testCommandWithoutResponseWithoutPayload", content);
            await response.AssertStatusCode(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();

            Assert.IsEmpty(result);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConqueror();

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandler2>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddTransient<TestCommandHandlerWithoutPayload>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutPayload>();

            _ = services.AddConquerorCQS().ConfigureConqueror();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }
    }
}
