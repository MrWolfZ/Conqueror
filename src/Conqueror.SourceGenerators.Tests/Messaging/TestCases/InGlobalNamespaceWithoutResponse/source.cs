using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;

// ReSharper disable CheckNamespace

[Message]
public partial record GlobalTestMessageWithoutResponse;

public partial class GlobalTestMessageWithoutResponseHandler : GlobalTestMessageWithoutResponse.IHandler
{
    public Task Handle(GlobalTestMessageWithoutResponse message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record GlobalTestMessageWithoutResponse
{
    public partial interface IHandler;
}
