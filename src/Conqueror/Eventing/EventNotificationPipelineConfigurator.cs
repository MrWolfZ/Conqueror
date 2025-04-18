using System;
using System.Diagnostics;

namespace Conqueror.Eventing;

internal sealed class EventNotificationPipelineConfigurator(Delegate? configurePipeline)
{
    public void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
        where T : class, IEventNotification<T>
    {
        if (configurePipeline is null)
        {
            return;
        }

        var typedConfigure = configurePipeline as Action<IEventNotificationPipeline<T>>;

        Debug.Assert(typedConfigure is not null, $"configurePipeline was not of correct type; expected notification type '{typeof(T)}', actual '{configurePipeline?.GetType()}'");

        typedConfigure(pipeline);
    }
}
