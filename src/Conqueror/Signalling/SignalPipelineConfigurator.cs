using System;
using System.Diagnostics;

namespace Conqueror.Signalling;

internal sealed class SignalPipelineConfigurator(Delegate? configurePipeline)
{
    public void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
        where T : class, ISignal<T>
    {
        if (configurePipeline is null)
        {
            return;
        }

        var typedConfigure = configurePipeline as Action<ISignalPipeline<T>>;

        Debug.Assert(typedConfigure is not null, $"configurePipeline was not of correct type; expected signal type '{typeof(T)}', actual '{configurePipeline?.GetType()}'");

        typedConfigure(pipeline);
    }
}
