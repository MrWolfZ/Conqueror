namespace Conqueror.Examples.BlazorWebAssembly.Application;

public interface IEventHub
{
    Task PublishEvent<TEvent>(TEvent evt)
        where TEvent : class;
}
