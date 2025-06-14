using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalReceivers
{
    IServiceProvider ServiceProvider { get; }

    SignalReceiverExecutionHandle CombineExecutions(IReadOnlyCollection<SignalReceiverExecutionHandle> executionHandles);
}
