using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests
{
    [TestFixture]
    public class ApiDescriptionTests : TestBase
    {
        private IApiDescriptionGroupCollectionProvider ApiDescriptionProvider => Resolve<IApiDescriptionGroupCollectionProvider>();

        [Test]
        public void ApiDescriptionProvider_ReturnsCommandDescriptors()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "commands.Test");

            Assert.IsNotNull(commandApiDescription);
            Assert.AreEqual(HttpMethods.Post, commandApiDescription?.HttpMethod);
            Assert.AreEqual(200, commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(commandApiDescription?.GroupName);
            Assert.AreEqual(1, commandApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithoutResponse()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "commands.TestCommandWithoutResponse");

            Assert.IsNotNull(commandApiDescription);
            Assert.AreEqual(HttpMethods.Post, commandApiDescription?.HttpMethod);
            Assert.AreEqual(204, commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(commandApiDescription?.GroupName);
            Assert.AreEqual(1, commandApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithoutPayload()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "commands.TestCommandWithoutPayload");

            Assert.IsNotNull(commandApiDescription);
            Assert.AreEqual(HttpMethods.Post, commandApiDescription?.HttpMethod);
            Assert.AreEqual(200, commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(commandApiDescription?.GroupName);
            Assert.AreEqual(0, commandApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsCommandDescriptorsWithoutResponseWithoutPayload()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var commandApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "commands.TestCommandWithoutResponseWithoutPayload");

            Assert.IsNotNull(commandApiDescription);
            Assert.AreEqual(HttpMethods.Post, commandApiDescription?.HttpMethod);
            Assert.AreEqual(204, commandApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(commandApiDescription?.GroupName);
            Assert.AreEqual(0, commandApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsQueryDescriptors()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "queries.Test");

            Assert.IsNotNull(queryApiDescription);
            Assert.AreEqual(HttpMethods.Get, queryApiDescription?.HttpMethod);
            Assert.AreEqual(200, queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(queryApiDescription?.GroupName);
            Assert.AreEqual(1, queryApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsPostQueryDescriptors()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "queries.TestPost");

            Assert.IsNotNull(queryApiDescription);
            Assert.AreEqual(HttpMethods.Post, queryApiDescription?.HttpMethod);
            Assert.AreEqual(200, queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(queryApiDescription?.GroupName);
            Assert.AreEqual(1, queryApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsQueryDescriptorsWithoutPayload()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "queries.TestQueryWithoutPayload");

            Assert.IsNotNull(queryApiDescription);
            Assert.AreEqual(HttpMethods.Get, queryApiDescription?.HttpMethod);
            Assert.AreEqual(200, queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(queryApiDescription?.GroupName);
            Assert.AreEqual(0, queryApiDescription?.ParameterDescriptions.Count);
        }

        [Test]
        public void ApiDescriptionProvider_ReturnsPostQueryDescriptorsWithoutPayload()
        {
            var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
            var queryApiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "queries.TestPostQueryWithoutPayload");

            Assert.IsNotNull(queryApiDescription);
            Assert.AreEqual(HttpMethods.Post, queryApiDescription?.HttpMethod);
            Assert.AreEqual(200, queryApiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single());
            Assert.IsNull(queryApiDescription?.GroupName);
            Assert.AreEqual(0, queryApiDescription?.ParameterDescriptions.Count);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorCQSHttpControllers();

            _ = services.AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandler2>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddTransient<TestCommandHandlerWithoutPayload>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutPayload>();

            _ = services.AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddTransient<TestQueryHandlerWithoutPayload>()
                        .AddTransient<TestPostQueryHandler>()
                        .AddTransient<TestPostQueryHandlerWithoutPayload>();

            _ = services.AddConquerorCQS().ConfigureConqueror();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }
    }
}
