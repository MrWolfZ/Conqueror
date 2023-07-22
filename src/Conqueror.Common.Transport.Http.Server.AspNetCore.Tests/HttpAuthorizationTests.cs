using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Conqueror.Common.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
public sealed class HttpAuthorizationTests : TestBase
{
    private const string TestUserId = "AuthorizationTestUser";

    private readonly TestData testData = new();
    
    [Test]
    public async Task GivenGetEndpointThatThrowsOperationTypeAuthorizationFailedException_WhenExecutingCommand_RequestFailsWithForbidden()
    {
        testData.ExceptionToThrow = new ConquerorOperationTypeAuthorizationFailedException("test", ConquerorAuthorizationResult.Failure("test"));

        using var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new("/api/test?payload=10", UriKind.Relative),
        };

        WithAuthenticatedPrincipal(requestMessage.Headers, TestUserId);

        var response = await HttpClient.SendAsync(requestMessage);
        await response.AssertStatusCode(HttpStatusCode.Forbidden);
    }
    
    [Test]
    public async Task GivenPostEndpointThatThrowsOperationTypeAuthorizationFailedException_WhenExecutingCommand_RequestFailsWithForbidden()
    {
        testData.ExceptionToThrow = new ConquerorOperationTypeAuthorizationFailedException("test", ConquerorAuthorizationResult.Failure("test"));

        using var content = JsonContent.Create(new TestRequest { Payload = 10 });
        using var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new("/api/test", UriKind.Relative),
            Content = content,
        };

        WithAuthenticatedPrincipal(requestMessage.Headers, TestUserId);

        var response = await HttpClient.SendAsync(requestMessage);
        await response.AssertStatusCode(HttpStatusCode.Forbidden);
    }
    
    [Test]
    public async Task GivenGetEndpointThatThrowsOperationPayloadAuthorizationFailedException_WhenExecutingCommand_RequestFailsWithForbidden()
    {
        testData.ExceptionToThrow = new ConquerorOperationPayloadAuthorizationFailedException("test", ConquerorAuthorizationResult.Failure("test"));

        using var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new("/api/test?payload=10", UriKind.Relative),
        };

        WithAuthenticatedPrincipal(requestMessage.Headers, TestUserId);

        var response = await HttpClient.SendAsync(requestMessage);
        await response.AssertStatusCode(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GivenPostEndpointThatThrowsOperationPayloadAuthorizationFailedException_WhenExecutingCommand_RequestFailsWithForbidden()
    {
        testData.ExceptionToThrow = new ConquerorOperationPayloadAuthorizationFailedException("test", ConquerorAuthorizationResult.Failure("test"));

        using var content = JsonContent.Create(new TestRequest { Payload = 10 });
        using var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new("/api/test", UriKind.Relative),
            Content = content,
        };

        WithAuthenticatedPrincipal(requestMessage.Headers, TestUserId);

        var response = await HttpClient.SendAsync(requestMessage);
        await response.AssertStatusCode(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GivenAspDefaultAuthorizationPolicy_WhenExecutingGetRequestWithUserWhichWouldFailAspAuthorization_RequestIsPassedThroughToEndpoint()
    {
        using var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new("/api/test?payload=10", UriKind.Relative),
        };

        WithAuthenticatedPrincipal(requestMessage.Headers, "unauthorized_user");

        var response = await HttpClient.SendAsync(requestMessage);
        await response.AssertSuccessStatusCode();
        
        Assert.That(testData.EndpointWasCalled, Is.True);
    }

    [Test]
    public async Task GivenAspDefaultAuthorizationPolicy_WhenExecutingPostRequestWithUserWhichWouldFailAspAuthorization_RequestIsPassedThroughToEndpoint()
    {
        using var content = JsonContent.Create(new TestRequest { Payload = 10 });
        using var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new("/api/test", UriKind.Relative),
            Content = content,
        };

        WithAuthenticatedPrincipal(requestMessage.Headers, "unauthorized_user");

        var response = await HttpClient.SendAsync(requestMessage);
        await response.AssertSuccessStatusCode();
        
        Assert.That(testData.EndpointWasCalled, Is.True);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var applicationPartManager = new ApplicationPartManager();
        applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
        applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

        _ = services.AddSingleton(applicationPartManager);
        _ = services.AddMvc();

        _ = services.AddAuthorization(o => o.DefaultPolicy = new AuthorizationPolicyBuilder().RequireUserName(TestUserId).Build());

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
        _ = app.UseAuthorization();

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

            testData.EndpointWasCalled = true;

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

            testData.EndpointWasCalled = true;

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

        public bool EndpointWasCalled { get; set; }
    }
}
