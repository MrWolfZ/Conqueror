using System;

namespace Conqueror.Eventing
{
    public interface IEventObserverPipelineBuilder
    {
        IServiceProvider ServiceProvider { get; }

        IEventObserverPipelineBuilder Use<TMiddleware>()
            where TMiddleware : IEventObserverMiddleware;
        
        IEventObserverPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IEventObserverMiddleware<TConfiguration>;
        
        IEventObserverPipelineBuilder Without<TMiddleware>()
            where TMiddleware : IEventObserverMiddleware;
        
        IEventObserverPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : IEventObserverMiddleware<TConfiguration>;
        
        IEventObserverPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
            where TMiddleware : IEventObserverMiddleware<TConfiguration>;
        
        IEventObserverPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
            where TMiddleware : IEventObserverMiddleware<TConfiguration>;
    }
}
