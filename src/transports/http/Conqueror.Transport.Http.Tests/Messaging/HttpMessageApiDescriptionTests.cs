namespace Conqueror.Transport.Http.Tests.Messaging;

using System.Globalization;

[TestFixture]
public sealed class HttpMessageApiDescriptionTests
{
    [Test]
    [TestCaseSource(nameof(CreateTestCases))]
    public async Task GivenTestHttpMessage_WhenRegisteringControllers_RegistersTheCorrectApiDescription(
        HttpMessageConformityExecutionSuccessTestCase testCase,
        Type messageType,
        Type? responseType
    )
    {
        await using var host = testCase.CreateTestHost();

        await using var receiverHost = await host.CreateReceiverTestHost(host.TestTimeoutToken);

        var apiDescriptionProvider = receiverHost.Resolve<IApiDescriptionGroupCollectionProvider>();

        var messageApiDescription = apiDescriptionProvider
            .ApiDescriptionGroups.Items.SelectMany(i => i.Items)
            .FirstOrDefault(d =>
                string.Equals(
                    d.ActionDescriptor.AttributeRouteInfo?.Name
                    ?? d.ActionDescriptor.EndpointMetadata.OfType<EndpointNameMetadata>()
                        .FirstOrDefault()
                        ?.EndpointName,
                    testCase.EndpointName ?? messageType.Name,
                    StringComparison.Ordinal
                )
            );

        if (testCase.IsOmittedFromApiDescriptions || !testCase.HandlerIsEnabled)
        {
            Assert.That(messageApiDescription, Is.Null);

            return;
        }

        Assert.That(messageApiDescription, Is.Not.Null);
        Assert.That(messageApiDescription.HttpMethod, Is.EqualTo(testCase.HttpMethod));
        Assert.That(
            messageApiDescription.RelativePath,
            Is.EqualTo((testCase.Template ?? testCase.FullPath).TrimStart(trimChar: '/'))
        );
        Assert.That(
            messageApiDescription.SupportedResponseTypes.Select(t => t.StatusCode),
            Is.EquivalentTo(new[] { testCase.SuccessStatusCode })
        );
        Assert.That(
            messageApiDescription.SupportedResponseTypes.Select(t => t.Type),
            Is.EquivalentTo(new[] { responseType ?? typeof(void) })
        );
        Assert.That(messageApiDescription.GroupName, Is.EqualTo(testCase.ApiGroupName));
        Assert.That(messageApiDescription.ParameterDescriptions, Has.Count.EqualTo(testCase.ParameterCount));

        var messageMediaTypes = messageApiDescription.SupportedRequestFormats.Select(f => f.MediaType);
        Assert.That(
            messageMediaTypes,
            testCase.MessageContentType is null ? Is.Empty : Is.EquivalentTo(new[] { testCase.MessageContentType })
        );

        var responseMediaTypes = messageApiDescription
            .SupportedResponseTypes.SelectMany(t => t.ApiResponseFormats)
            .Select(f => f.MediaType);
        Assert.That(
            responseMediaTypes,
            testCase.ResponseContentType is null ? Is.Empty : Is.EquivalentTo(new[] { testCase.ResponseContentType })
        );

        if (
            testCase is { ParameterCount: 1 }
            && string.Equals(testCase.HttpMethod, MethodNames.Post, StringComparison.Ordinal)
        )
        {
            Assert.That(messageApiDescription.ParameterDescriptions[0].Type, Is.EqualTo(messageType));
        }

        // if (messageType.IsAssignableTo(typeof(TestMessageWithGetWithOptionalPayload))
        //     || messageType.IsAssignableTo(typeof(TestMessageWithGetWithPrimaryConstructorWithOptionalParameters)))
        // {
        //     Assert.That(messageApiDescription.ParameterDescriptions.Select(p => p.IsRequired), Is.EqualTo(new[] { false, false }));
        // }
    }

    [Test]
    [TestCaseSource(nameof(CreateTestCases))]
    public async Task GivenTestHttpMessage_WhenRegisteringControllers_SwashbuckleGeneratesTheCorrectDoc(
        HttpMessageConformityExecutionSuccessTestCase testCase,
        Type messageType,
        Type? responseType
    )
    {
        await using var host = testCase.CreateTestHost();

        await using var receiverHost = await host.CreateReceiverTestHost(
            host.TestTimeoutToken,
            services =>
            {
                _ = services
                    .AddEndpointsApiExplorer()
                    .AddSwaggerGen(c =>
                    {
                        c.DocInclusionPredicate((_, _) => true);
                    });
            },
            app =>
            {
                _ = app.UseSwagger();
            }
        );

        var swaggerContent = await receiverHost.HttpClient.GetStringAsync(
            new Uri("/swagger/v1/swagger.json", UriKind.Relative),
            host.TestTimeoutToken
        );

        Assert.That(swaggerContent, Is.Not.Null.Or.Empty);

        var sw = receiverHost.Resolve<ISwaggerProvider>();
        var doc = sw.GetSwagger("v1", host: null, "/");

        // strips parameter type annotations, e.g. {payload:int} becomes {payload}
        var templateNormalizerRegex = new Regex("{([^:}]+):?[^}]*}");

        var expectedPath = (
            testCase.Template is not null
                ? templateNormalizerRegex.Replace(testCase.Template, "{$1}")
                : testCase.FullPath
        ).TrimStart(trimChar: '/');
        var path = doc
            .Paths.Where(p => string.Equals(p.Key.TrimStart(trimChar: '/'), expectedPath, StringComparison.Ordinal))
            .Select(p => p.Value)
            .SingleOrDefault();

        if (testCase.IsOmittedFromApiDescriptions || !testCase.HandlerIsEnabled)
        {
            Assert.That(path, Is.Null);

            return;
        }

        Assert.That(path, Is.Not.Null);
        Assert.That(path.Operations, Has.Count.EqualTo(expected: 1));

        var operation = path.Operations.Single();

        Assert.That(ToHttpMethodString(operation.Key), Is.EqualTo(testCase.HttpMethod));
        Assert.That(operation.Value.OperationId, Is.EqualTo(testCase.EndpointName ?? messageType.Name));

        var expectedTags = new[] { testCase.ApiGroupName ?? testCase.EndpointName ?? messageType.Name };
        Assert.That(operation.Value.Tags.Select(t => t.Name).ToList(), Is.EqualTo(expectedTags));

        Assert.That(
            operation.Value.Parameters,
            Has.Count.EqualTo(
                string.Equals(testCase.HttpMethod, MethodNames.Get, StringComparison.Ordinal)
                    ? testCase.ParameterCount
                    : 0
            )
        );

        var responseDescriptor = operation.Value.Responses.Single();
        Assert.That(
            responseDescriptor.Key,
            Is.EqualTo(testCase.SuccessStatusCode.ToString(CultureInfo.InvariantCulture))
        );

        Assert.That(
            operation.Value.RequestBody?.Content.Keys,
            testCase.MessageContentType is null
                ? Is.Null.Or.Empty
                : Is.EquivalentTo(new[] { testCase.MessageContentType })
        );

        Assert.That(
            responseDescriptor.Value.Content.Keys,
            testCase.ResponseContentType is null ? Is.Empty : Is.EquivalentTo(new[] { testCase.ResponseContentType })
        );

        if (testCase.ResponseContentType is not null)
        {
            var responseMediaType = responseDescriptor.Value.Content.Values.Single();

            if ((responseType?.IsAssignableTo(typeof(IEnumerable))) is true)
            {
                Assert.That(responseMediaType.Schema.Reference?.Id, Is.Null);
                Assert.That(responseMediaType.Schema.Type, Is.EqualTo("array"));
                Assert.That(
                    responseMediaType.Schema.Items?.Reference?.Id,
                    Is.EqualTo(responseType.GetElementType()?.Name ?? responseType.GetGenericArguments()[0].Name)
                );
            }
            else
            {
                Assert.That(responseMediaType.Schema.Reference?.Id, Is.EqualTo(responseType?.Name));
            }
        }
    }

    private static IEnumerable<TestCaseData> CreateTestCases() =>
        HttpMessageTestCases
            .CreateSuccessTestCases()
            .Where(tc => tc.SingleMessageType is not null)
            .Select(tc => new TestCaseData(tc, tc.SingleMessageType, tc.SingleResponseType).SetName(tc.Name));

    private static string ToHttpMethodString(OperationType operationType) =>
        operationType switch
        {
            OperationType.Get => "GET",
            OperationType.Post => "POST",
            OperationType.Put => "PUT",
            OperationType.Delete => "DELETE",
            OperationType.Head => "HEAD",
            OperationType.Options => "OPTIONS",
            OperationType.Trace => "TRACE",
            OperationType.Patch => "PATCH",
            _ => throw new ArgumentOutOfRangeException(nameof(operationType), operationType, message: null),
        };
}
