using System.Reflection;
using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.FileSystem.Tests.Messaging;

public sealed class FileSystemMessageConformityExecutionSuccessTestCase
    : FileSystemMessageConformityExecutionTestCase,
      IMessageTransportConformityExecutionSuccessTestCase<FileSystemMessageTransportConformityTestHost>
{
    private readonly IReadOnlyCollection<string?>? messagePayloads;
    private readonly IReadOnlyCollection<string>? responsePayloads;

    public bool ShouldCompleteImmediately => !HandlerIsEnabled;

    public bool HandlerIsEnabled { get; init; } = true;

    public string? Tag { get; init; }

    public IReadOnlyCollection<string?> MessagePayloads
    {
        get => messagePayloads
               ?? ExpectedReceivedMessages
                  .Select(m => $"{{\"payload\":{m.GetType().GetProperty("Payload", BindingFlags.Public | BindingFlags.Instance)?.GetValue(m)}}}")
                  .ToArray();
        init => messagePayloads = value;
    }

    public IReadOnlyCollection<string> ResponsePayloads
    {
        get => responsePayloads
               ?? ExpectedResponses
                  .Select(r => $"{{\"payload\":{r.GetType().GetProperty("Payload", BindingFlags.Public | BindingFlags.Instance)?.GetValue(r)}}}")
                  .ToArray();
        init => responsePayloads = value;
    }

    public Func<FileSystemMessageTransportConformityTestHost, Task>? BeforeSend { get; init; }

    public Func<FileSystemMessageTransportConformityTestHost, Task>? AfterMessagesAreReceived { get; init; }

    public Action<FileSystemMessageTransportConformityTestHost, IFileSystemMessageReceiver>? ConfigureReceiverFn { get; init; }

    Task IMessageTransportConformityExecutionSuccessTestCase<FileSystemMessageTransportConformityTestHost>.BeforeSend(
        FileSystemMessageTransportConformityTestHost host)
        => BeforeSend?.Invoke(host) ?? Task.CompletedTask;

    async Task IMessageTransportConformityExecutionSuccessTestCase<FileSystemMessageTransportConformityTestHost>.AfterMessagesAreReceived(
        FileSystemMessageTransportConformityTestHost testHost)
    {
        if (AfterMessagesAreReceived is not null)
        {
            await AfterMessagesAreReceived.Invoke(testHost);
        }
    }

    public override void ConfigureReceiver(FileSystemMessageTransportConformityTestHost host, IFileSystemMessageReceiver receiver)
    {
        ConfigureReceiverFn?.Invoke(host, receiver);

        base.ConfigureReceiver(host, receiver);
    }
}
