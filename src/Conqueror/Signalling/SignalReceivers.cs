using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalReceivers(IServiceProvider serviceProvider) : ISignalReceivers
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ReceiverExecutionHandle CombineExecutions(IReadOnlyCollection<ReceiverExecutionHandle> executionHandles)
    {
        var combinedRun = new ReceiverExecutionHandle(executionHandles);
        HandleErrors(combinedRun);

        return combinedRun;

        static async void HandleErrors(ReceiverExecutionHandle combinedExecutionHandle)
        {
            try
            {
                // at this point all receivers are running, so we can observe their completion tasks for any errors,
                // in which case we cancel all remaining executions; this in turn will cancel the overall execution
                // and expose the error to the caller through the completion task of the returned handle
                var executionsToObserve = combinedExecutionHandle.InnerHandles!.ToList();

                while (executionsToObserve.Count > 0)
                {
                    var completedTask = await Task.WhenAny(executionsToObserve.Select(r => r.CompletionTask)).ConfigureAwait(false);

                    if (completedTask.IsFaulted)
                    {
                        // this will trigger the `finally` block below, which will cancel all remaining executions
                        return;
                    }

                    _ = executionsToObserve.Remove(executionsToObserve.First(r => r.CompletionTask == completedTask));
                }
            }
            catch
            {
                // this should never happen (i.e. there is no known circumstance under which
                // the block above throws), but if it does, we want to cancel all remaining executions
            }
            finally
            {
                // cancel all remaining executions
                await combinedExecutionHandle.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
