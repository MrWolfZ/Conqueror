using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalReceivers
{
    IServiceProvider ServiceProvider { get; }

    SignalReceiverRun CombineRuns(IReadOnlyCollection<SignalReceiverRun> runs);
}
