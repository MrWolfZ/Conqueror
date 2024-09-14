using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.Common.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
public sealed class HttpAuthenticationTests : TestBase
{
    private const string TestUserId = "AuthenticationTestUser";

    private readonly TestData testData = new();

    [Test]
    public async Task GivenGetEndpoint_WhenExecutingWithoutAuthenticatedPrincipal_EndpointObservesUnauthenticatedPrincipal()
    {
        var response = await HttpClient.GetAsync("/api/test?payload=10");
        await response.AssertSuccessStatusCode();

        Assert.That(testData.ObservedPrincipal?.Identity?.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task GivenPostEndpoint_WhenExecutingWithoutAuthenticatedPrincipal_EndpointObservesUnauthenticatedPrincipal()
    {
        using var content = JsonContent.Create(new TestRequest { Payload = 10 });
        var response = await HttpClient.PostAsync("/api/test", content);
        await response.AssertSuccessStatusCode();

        Assert.That(testData.ObservedPrincipal?.Identity?.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task GivenGetEndpoint_WhenExecutingWithAuthenticatedPrincipal_EndpointObservesCorrectPrincipal()
    {
        using var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new("/api/test?payload=10", UriKind.Relative),
        };

        WithAuthenticatedPrincipal(requestMessage.Headers, TestUserId);

        var response = await HttpClient.SendAsync(requestMessage);
        await response.AssertSuccessStatusCode();

        Assert.That(testData.ObservedPrincipal, Is.Not.Null);
        Assert.That(testData.ObservedPrincipal?.Identity?.Name, Is.EqualTo(TestUserId));
    }

    [Test]
    public async Task GivenPostEndpoint_WhenExecutingWithAuthenticatedPrincipal_EndpointObservesCorrectPrincipal()
    {
        using var content = JsonContent.Create(new TestRequest { Payload = 10 });
        using var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new("/api/test", UriKind.Relative),
            Content = content,
        };

        WithAuthenticatedPrincipal(requestMessage.Headers, TestUserId);

        var response = await HttpClient.SendAsync(requestMessage);
        await response.AssertSuccessStatusCode();

        Assert.That(testData.ObservedPrincipal, Is.Not.Null);
        Assert.That(testData.ObservedPrincipal?.Identity?.Name, Is.EqualTo(TestUserId));
    }

    [Test]
    public async Task GivenGetEndpointThatThrowsMissingPrincipalException_WhenExecutingWithoutAuthenticatedPrincipal_RequestFailsWithUnauthorized()
    {
        testData.ExceptionToThrow = new ConquerorAuthenticationMissingPrincipalException("test");

        var response = await HttpClient.GetAsync("/api/test?payload=10");
        await response.AssertStatusCode(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GivenPostEndpointThatThrowsMissingPrincipalException_WhenExecutingWithoutAuthenticatedPrincipal_RequestFailsWithUnauthorized()
    {
        testData.ExceptionToThrow = new ConquerorAuthenticationMissingPrincipalException("test");

        using var content = JsonContent.Create(new TestRequest { Payload = 10 });
        var response = await HttpClient.PostAsync("/api/test", content);
        await response.AssertStatusCode(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GivenGetEndpointThatThrowsUnauthenticatedPrincipalException_WhenExecutingWithoutAuthenticatedPrincipal_RequestFailsWithUnauthorized()
    {
        testData.ExceptionToThrow = new ConquerorAuthenticationUnauthenticatedPrincipalException("test");

        var response = await HttpClient.GetAsync("/api/test?payload=10");
        await response.AssertStatusCode(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GivenPostEndpointThatThrowsUnauthenticatedPrincipalException_WhenExecutingWithoutAuthenticatedPrincipal_RequestFailsWithUnauthorized()
    {
        testData.ExceptionToThrow = new ConquerorAuthenticationUnauthenticatedPrincipalException("test");

        using var content = JsonContent.Create(new TestRequest { Payload = 10 });
        var response = await HttpClient.PostAsync("/api/test", content);
        await response.AssertStatusCode(HttpStatusCode.Unauthorized);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var applicationPartManager = new ApplicationPartManager();
        applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
        applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

        _ = services.AddSingleton(applicationPartManager);
        _ = services.AddMvc();

        _ = services.AddSingleton(testData);
        _ = services.AddConquerorContext();
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.Use(async (ctx, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await ctx.Response.WriteAsync(ex.ToString());
            }
        });

        _ = app.UseAuthentication();

        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    private sealed record TestRequest
    {
        public int Payload { get; init; }
    }

    private sealed record TestRequestResponse
    {
        public int Payload { get; init; }
    }

    [ApiController]
    private sealed class TestController : ControllerBase
    {
        private readonly TestData testData;

        public TestController(TestData data)
        {
            testData = data;
        }

        [HttpGet("/api/test")]
        public async Task<TestRequestResponse> TestGet([FromQuery] TestRequest request, CancellationToken cancellationToken)
        {
            await Task.Yield();

            testData.ObservedPrincipal = new ConquerorAuthenticationContext().CurrentPrincipal;

            if (testData.ExceptionToThrow is not null)
            {
                throw testData.ExceptionToThrow;
            }

            cancellationToken.ThrowIfCancellationRequested();

            return new() { Payload = request.Payload + 1 };
        }

        [HttpPost("/api/test")]
        public async Task<TestRequestResponse> TestPost(TestRequest request, CancellationToken cancellationToken)
        {
            await Task.Yield();

            testData.ObservedPrincipal = new ConquerorAuthenticationContext().CurrentPrincipal;

            if (testData.ExceptionToThrow is not null)
            {
                throw testData.ExceptionToThrow;
            }

            cancellationToken.ThrowIfCancellationRequested();

            return new() { Payload = request.Payload + 1 };
        }
    }

    private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(TestControllerApplicationPart);

        public IEnumerable<TypeInfo> Types { get; } = new[] { typeof(TestController).GetTypeInfo() };
    }

    private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestController);
    }

    private sealed class TestData
    {
        public Exception? ExceptionToThrow { get; set; }

        public ClaimsPrincipal? ObservedPrincipal { get; set; }
    }
}
