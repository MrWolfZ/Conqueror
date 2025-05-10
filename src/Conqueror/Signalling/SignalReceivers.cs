using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalReceivers(IServiceProvider serviceProvider) : ISignalReceivers
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public SignalReceiverRun CombineRuns(IReadOnlyCollection<SignalReceiverRun> runs)
    {
        var combinedRun = new SignalReceiverRun(runs);
        HandleErrors(combinedRun);

        return combinedRun;

        static async void HandleErrors(SignalReceiverRun combinedRun)
        {
            try
            {
                // at this point all receivers are running, so we can observe their completion tasks for any errors,
                // in which case we cancel all remaining runs; this in turn will cancel the overall run and expose
                // the error to the caller through the completion task of the returned run
                var runsToObserve = combinedRun.InnerRuns!.ToList();

                while (runsToObserve.Count > 0)
                {
                    var completedTask = await Task.WhenAny(runsToObserve.Select(r => r.CompletionTask)).ConfigureAwait(false);

                    if (completedTask.IsFaulted)
                    {
                        // this will trigger the `finally` block below, which will cancel all remaining runs
                        return;
                    }

                    _ = runsToObserve.Remove(runsToObserve.First(r => r.CompletionTask == completedTask));
                }
            }
            catch
            {
                // this should never happen (i.e. there is no known circumstance under which
                // the block above throws), but if it does, we want to cancel all remaining runs
            }
            finally
            {
                // cancel all remaining runs
                await combinedRun.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
