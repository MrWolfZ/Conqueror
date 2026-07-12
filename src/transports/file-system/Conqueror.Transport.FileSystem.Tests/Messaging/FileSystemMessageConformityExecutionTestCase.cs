namespace Conqueror.Transport.FileSystem.Tests.Messaging;

public abstract class FileSystemMessageConformityExecutionTestCase
    : FileSystemMessageConformityTestCase,
      IMessageTransportConformityExecutionTestCase<FileSystemMessageTransportConformityTestHost>
{
    private readonly Type? singleResponseType;

    public required IReadOnlyCollection<object> ExpectedResponses { get; init; }

    public Type? SingleMessageType
    {
        get
        {
            var distinctMessageTypes = ExpectedReceivedMessages.Select(m => m.GetType()).Distinct().ToArray();

            return distinctMessageTypes.Length is 1 ? distinctMessageTypes.Single() : null;
        }
    }

    public Type? SingleResponseType
    {
        get
        {
            if (singleResponseType is not null)
            {
                return singleResponseType;
            }

            var distinctResponseTypes = ExpectedResponses
                .Where(r => r is not UnitMessageResponse)
                .Select(m => m.GetType())
                .Distinct()
                .ToArray();

            return distinctResponseTypes.Length is 1 ? distinctResponseTypes.Single() : null;
        }
        init => singleResponseType = value;
    }

    public int NumOfReceivers { get; init; } = 1;

    public bool MessagesAreSentInParallel { get; init; }

    public required IReadOnlyCollection<object> ExpectedReceivedMessages { get; init; }

    IReadOnlyCollection<object> IMessageTransportConformityExecutionTestCase<FileSystemMessageTransportConformityTestHost>.ExpectedResponses =>
        ExpectedResponses.Where(r => r is not UnitMessageResponse).ToArray();
}
