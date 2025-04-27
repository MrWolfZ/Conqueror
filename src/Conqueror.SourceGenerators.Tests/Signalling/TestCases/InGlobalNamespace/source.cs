using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;

// ReSharper disable CheckNamespace

[Signal]
public partial record GlobalTestSignal;

public partial class GlobalTestSignalHandler : GlobalTestSignal.IHandler
{
    public Task Handle(GlobalTestSignal message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record GlobalTestSignal
{
    public partial interface IHandler;
}
