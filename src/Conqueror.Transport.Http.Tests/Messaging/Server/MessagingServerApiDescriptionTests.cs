using System.Net.Mime;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using static Conqueror.ConquerorTransportHttpConstants;
using static Conqueror.Transport.Http.Tests.Messaging.HttpTestMessages;

namespace Conqueror.Transport.Http.Tests.Messaging.Server;

[TestFixture]
public sealed class MessagingServerApiDescriptionTests
{
    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenTestHttpMessage_WhenRegisteringControllers_RegistersTheCorrectApiDescription<TMessage, TResponse, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where THandler : class, IGeneratedMessageHandler
    {
        await using var host = await HttpTransportTestHost.Create(
            services => services.RegisterMessageType<TMessage, TResponse, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse>(testCase));

        var apiDescriptionProvider = host.Resolve<IApiDescriptionGroupCollectionProvider>();

        var messageApiDescription = apiDescriptionProvider.ApiDescriptionGroups
                                                          .Items
                                                          .SelectMany(i => i.Items)
                                                          .FirstOrDefault(d => (d.ActionDescriptor.AttributeRouteInfo?.Name
                                                                                ?? d.ActionDescriptor.EndpointMetadata.OfType<EndpointNameMetadata>()
                                                                                    .FirstOrDefault()?.EndpointName)
                                                                               == (testCase.Name ?? testCase.MessageType.Name));

        Assert.That(messageApiDescription, Is.Not.Null);
        Assert.That(messageApiDescription.HttpMethod, Is.EqualTo(testCase.HttpMethod));
        Assert.That(messageApiDescription.RelativePath, Is.EqualTo((testCase.Template ?? testCase.FullPath).TrimStart('/')));
        Assert.That(messageApiDescription.SupportedResponseTypes.Select(t => t.StatusCode), Is.EquivalentTo(new[] { testCase.SuccessStatusCode }));
        Assert.That(messageApiDescription.SupportedResponseTypes.Select(t => t.Type), Is.EquivalentTo(new[] { testCase.ResponseType ?? typeof(void) }));
        Assert.That(messageApiDescription.GroupName, Is.EqualTo(testCase.ApiGroupName));
        Assert.That(messageApiDescription.ParameterDescriptions, Has.Count.EqualTo(testCase.ParameterCount));

        var isController = testCase.RegistrationMethod
            is MessageTestCaseRegistrationMethod.Controllers
            or MessageTestCaseRegistrationMethod.ExplicitController
            or MessageTestCaseRegistrationMethod.CustomController;

        var messageMediaTypes = messageApiDescription.SupportedRequestFormats.Select(f => f.MediaType);
        var expectedAcceptContentTypes = isController ? [MediaTypeNames.Application.Json, "application/*+json"] : new[] { testCase.MessageContentType };
        Assert.That(messageMediaTypes, testCase.MessageContentType is null ? Is.Empty : Is.EquivalentTo(expectedAcceptContentTypes));

        var responseMediaTypes = messageApiDescription.SupportedResponseTypes.SelectMany(t => t.ApiResponseFormats).Select(f => f.MediaType);
        Assert.That(responseMediaTypes, testCase.ResponseContentType is null ? Is.Empty : Is.EquivalentTo(new[] { testCase.ResponseContentType }));

        if (testCase is { ParameterCount: 1, HttpMethod: MethodPost })
        {
            Assert.That(messageApiDescription.ParameterDescriptions[0].Type, Is.EqualTo(testCase.MessageType));
        }
    }

    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenTestHttpMessage_WhenRegisteringControllers_SwashbuckleGeneratesTheCorrectDoc<TMessage, TResponse, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where THandler : class, IGeneratedMessageHandler
    {
        await using var host = await HttpTransportTestHost.Create(
            services =>
            {
                services.RegisterMessageType<TMessage, TResponse, THandler>(testCase);

                _ = services.AddEndpointsApiExplorer()
                            .AddSwaggerGen(c =>
                            {
                                c.DocInclusionPredicate((_, _) => true);
                            });
            },
            app =>
            {
                _ = app.UseSwagger();

                app.MapMessageEndpoints<TMessage, TResponse>(testCase);
            });

        var swaggerContent = await host.HttpClient.GetStringAsync("/swagger/v1/swagger.json");

        Assert.That(swaggerContent, Is.Not.Null.Or.Empty);

        var sw = host.Resolve<ISwaggerProvider>();
        var doc = sw.GetSwagger("v1", null, "/");

        // strips parameter type annotations, e.g. {payload:int} becomes {payload}
        var templateNormalizerRegex = new Regex("{([^:}]+):?[^}]*}");

        var expectedPath = (testCase.Template is not null ? templateNormalizerRegex.Replace(testCase.Template, "{$1}") : testCase.FullPath).TrimStart('/');
        var path = doc.Paths.Single(p => p.Key.TrimStart('/') == expectedPath);

        Assert.That(path.Value.Operations, Has.Count.EqualTo(1));

        var operation = path.Value.Operations.Single();

        Assert.That(ToHttpMethodString(operation.Key), Is.EqualTo(testCase.HttpMethod));
        Assert.That(operation.Value.OperationId, Is.EqualTo(testCase.Name ?? testCase.MessageType.Name));

        var expectedTags = testCase.RegistrationMethod is MessageTestCaseRegistrationMethod.CustomController
            ? new[] { "TestHttpMessage" }
            : [testCase.ApiGroupName ?? testCase.Name ?? testCase.MessageType.Name];

        Assert.That(operation.Value.Tags.Select(t => t.Name).ToList(), Is.EqualTo(expectedTags));

        Assert.That(operation.Value.Parameters, Has.Count.EqualTo(testCase.HttpMethod == MethodGet ? testCase.ParameterCount : 0));

        var responseDescriptor = operation.Value.Responses.Single();
        Assert.That(responseDescriptor.Key, Is.EqualTo(testCase.SuccessStatusCode.ToString()));

        var isController = testCase.RegistrationMethod
            is MessageTestCaseRegistrationMethod.Controllers
            or MessageTestCaseRegistrationMethod.ExplicitController;

        var expectedAcceptContentTypes = isController ? [MediaTypeNames.Application.Json, "application/*+json"] : new[] { testCase.MessageContentType };
        Assert.That(operation.Value.RequestBody?.Content.Keys, testCase.MessageContentType is null ? Is.Null.Or.Empty : Is.EquivalentTo(expectedAcceptContentTypes));

        Assert.That(responseDescriptor.Value.Content.Keys, testCase.ResponseContentType is null ? Is.Empty : Is.EquivalentTo(new[] { testCase.ResponseContentType }));
    }

    private static string ToHttpMethodString(OperationType operationType) => operationType switch
    {
        OperationType.Get => "GET",
        OperationType.Post => "POST",
        OperationType.Put => "PUT",
        OperationType.Delete => "DELETE",
        OperationType.Head => "HEAD",
        OperationType.Options => "OPTIONS",
        OperationType.Trace => "TRACE",
        OperationType.Patch => "PATCH",
        _ => throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null)
    };
}
