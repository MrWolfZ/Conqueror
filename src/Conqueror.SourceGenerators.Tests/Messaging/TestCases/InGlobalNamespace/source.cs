using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;

// ReSharper disable CheckNamespace

[Message<GlobalTestMessageResponse>]
public partial record GlobalTestMessage;

public record GlobalTestMessageResponse;

public partial class GlobalTestMessageHandler : GlobalTestMessage.IHandler
{
    public Task<GlobalTestMessageResponse> Handle(GlobalTestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record GlobalTestMessage
{
    public partial interface IHandler;
}
