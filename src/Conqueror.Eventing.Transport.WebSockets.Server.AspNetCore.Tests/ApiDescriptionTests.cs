using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore.Tests;

[TestFixture]
public class ApiDescriptionTests : TestBase
{
    private IApiDescriptionGroupCollectionProvider ApiDescriptionProvider => Resolve<IApiDescriptionGroupCollectionProvider>();

    private const string CustomEndpointPath = "api/customEvents";

    [Test]
    public void ApiDescriptionProvider_ReturnsEndpointDescriptor()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var apiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "events");

        Assert.That(apiDescription, Is.Not.Null);
        Assert.That(apiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(apiDescription?.RelativePath, Is.EqualTo("api/events"));
        Assert.That(apiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(apiDescription?.GroupName, Is.EqualTo("Events"));
        Assert.That(apiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    [Test]
    public void ApiDescriptionProvider_ReturnsEndpointDescriptorWithCustomPath()
    {
        var apiDescriptions = ApiDescriptionProvider.ApiDescriptionGroups.Items.SelectMany(i => i.Items);
        var apiDescription = apiDescriptions.FirstOrDefault(d => d.ActionDescriptor.AttributeRouteInfo?.Name == "events");

        Assert.That(apiDescription, Is.Not.Null);
        Assert.That(apiDescription?.HttpMethod, Is.EqualTo(HttpMethods.Get));
        Assert.That(apiDescription?.RelativePath, Is.EqualTo(CustomEndpointPath));
        Assert.That(apiDescription?.SupportedResponseTypes.Select(t => t.StatusCode).Single(), Is.EqualTo(200));
        Assert.That(apiDescription?.GroupName, Is.EqualTo("Events"));
        Assert.That(apiDescription?.ParameterDescriptions.Count, Is.EqualTo(1));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorEventingWebSocketsControllers(o =>
        {
            var isCustomEndpointPathTest = TestContext.CurrentContext.Test.Name == nameof(ApiDescriptionProvider_ReturnsEndpointDescriptorWithCustomPath);

            if (isCustomEndpointPathTest)
            {
                o.EndpointPath = CustomEndpointPath;
            }
        });

        _ = services.AddConquerorEventingWebSocketsTransportPublisher();
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }
}
