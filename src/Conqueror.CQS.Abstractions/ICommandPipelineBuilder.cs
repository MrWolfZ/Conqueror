using System;

namespace Conqueror
{
    public interface ICommandPipelineBuilder
    {
        IServiceProvider ServiceProvider { get; }

        ICommandPipelineBuilder Use<TMiddleware>()
            where TMiddleware : ICommandMiddleware;
        
        ICommandPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : ICommandMiddleware<TConfiguration>;
        
        ICommandPipelineBuilder Without<TMiddleware>()
            where TMiddleware : ICommandMiddleware;
        
        ICommandPipelineBuilder Without<TMiddleware, TConfiguration>()
            where TMiddleware : ICommandMiddleware<TConfiguration>;
        
        ICommandPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
            where TMiddleware : ICommandMiddleware<TConfiguration>;
        
        ICommandPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
            where TMiddleware : ICommandMiddleware<TConfiguration>;
        
        ICommandPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
            where TMiddleware : ICommandMiddleware<TConfiguration>;
    }
}
