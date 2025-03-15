using System.Net.Mime;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using static Conqueror.ConquerorTransportHttpConstants;

namespace Conqueror.Transport.Http.Tests.Messaging.Server;

[TestFixture]
public sealed class MessagingServerApiDescriptionTests
{
    [Test]
    [TestCaseSource(typeof(TestMessages), nameof(TestMessages.GenerateTestCases))]
    public async Task GivenTestHttpMessage_WhenRegisteringControllers_RegistersTheCorrectApiDescription(TestMessages.MessageTestCase testCase)
    {
        await using var host = await TestHost.Create(services => services.RegisterMessageType(testCase), app => app.MapMessageEndpoints(testCase));

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
            is TestMessages.MessageTestCaseRegistrationMethod.Controllers
            or TestMessages.MessageTestCaseRegistrationMethod.ExplicitController
            or TestMessages.MessageTestCaseRegistrationMethod.CustomController;

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
}
