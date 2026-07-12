namespace Conqueror.Transport.Http.Tests.Messaging;

public sealed class HttpMessageConformityExecutionSuccessTestCase
    : HttpMessageConformityExecutionTestCase,
      IMessageTransportConformityExecutionSuccessTestCase<HttpMessageTransportConformityTestHost>
{
    private readonly string? messageContentType;
    private readonly bool messageContentTypeWasSet;
    private readonly IReadOnlyCollection<string?>? messagePayloads;
    private readonly int? parameterCount;
    private readonly IReadOnlyCollection<string?>? queryStrings;
    private readonly string? responseContentType;
    private readonly bool responseContentTypeWasSet;
    private readonly IReadOnlyCollection<string>? responsePayloads;

    public bool IsOmittedFromApiDescriptions { get; init; }

    public bool HandlerIsEnabled { get; init; } = true;

    public string HttpMethod { get; init; } = MethodNames.Post;

    public required string FullPath { get; init; }

    public string? Template { get; init; }

    public int SuccessStatusCode { get; init; } = 200;

    public string? EndpointName { get; init; }

    public string? ApiGroupName { get; init; }

    public int ParameterCount
    {
        get =>
            parameterCount
            ?? (
                string.Equals(HttpMethod, MethodNames.Post, StringComparison.Ordinal)
                    ? 1
                    : throw new InvalidOperationException("Parameter count is not set for this test case.")
            );
        init => parameterCount = value;
    }

    public string? MessageContentType
    {
        get
        {
            if (messageContentTypeWasSet)
            {
                return messageContentType;
            }

            return SingleMessageType is not null ? MediaTypeNames.Application.Json : null;
        }
        init
        {
            messageContentType = value;
            messageContentTypeWasSet = true;
        }
    }

    public string? ResponseContentType
    {
        get
        {
            if (responseContentTypeWasSet)
            {
                return responseContentType;
            }

            return SingleResponseType is not null ? MediaTypeNames.Application.Json : null;
        }
        init
        {
            responseContentType = value;
            responseContentTypeWasSet = true;
        }
    }

    public IReadOnlyCollection<string?> QueryStrings
    {
        get => queryStrings ?? ExpectedReceivedMessages.Select(string? (_) => null).ToArray();
        init => queryStrings = value;
    }

    public IReadOnlyCollection<string?> MessagePayloads
    {
        get =>
            messagePayloads
            ?? ExpectedReceivedMessages
                .Select(m =>
                    $"{{\"payload\":{m.GetType().GetProperty("Payload", BindingFlags.Public | BindingFlags.Instance)?.GetValue(m)}}}"
                )
                .ToArray();
        init => messagePayloads = value;
    }

    public IReadOnlyCollection<string> ResponsePayloads
    {
        get =>
            responsePayloads
            ?? ExpectedResponses
                .Select(r =>
                    $"{{\"payload\":{r.GetType().GetProperty("Payload", BindingFlags.Public | BindingFlags.Instance)?.GetValue(r)}}}"
                )
                .ToArray();
        init => responsePayloads = value;
    }

    public Func<HttpMessageTransportConformityTestHost, Task>? BeforeSend { get; init; }

    public Func<HttpMessageTransportConformityTestHost, Task>? AfterMessagesAreReceived { get; init; }

    public Action<HttpMessageTransportConformityTestHost, IHttpMessageReceiver>? ConfigureReceiverFn { get; init; }

    public bool ShouldCompleteImmediately => !HandlerIsEnabled;

    Task IMessageTransportConformityExecutionSuccessTestCase<HttpMessageTransportConformityTestHost>.BeforeSend(
        HttpMessageTransportConformityTestHost host
    ) => BeforeSend?.Invoke(host) ?? Task.CompletedTask;

    async Task IMessageTransportConformityExecutionSuccessTestCase<HttpMessageTransportConformityTestHost>.AfterMessagesAreReceived(
        HttpMessageTransportConformityTestHost host
    )
    {
        if (AfterMessagesAreReceived is not null)
        {
            await AfterMessagesAreReceived.Invoke(host);
        }

        Assert.That(
            host.ReceiverHost.ReceivedQueryStringsOnServer.Select(s => string.IsNullOrWhiteSpace(s) ? null : s),
            Is.EquivalentTo(QueryStrings.Select(s => string.IsNullOrWhiteSpace(s) ? null : s))
        );
    }

    public override void ConfigureReceiver(HttpMessageTransportConformityTestHost host, IHttpMessageReceiver receiver)
    {
        ConfigureReceiverFn?.Invoke(host, receiver);

        base.ConfigureReceiver(host, receiver);
    }
}
