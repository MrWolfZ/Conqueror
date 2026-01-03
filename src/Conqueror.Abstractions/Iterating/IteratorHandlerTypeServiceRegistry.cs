#pragma warning disable SA1201 // ElementsMustAppearInTheCorrectOrder

namespace Conqueror;

public static class IteratorHandlerTypeServiceRegistry
{
    private static readonly List<Action<IIteratorHandlerServiceRegisterable>> RegistrationActions = [];

    public static void RegisterHandlerType<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >()
        where THandler : class, IIteratorHandler => RegisterHandlerTypeInternal<THandler>();

    internal static void RunWithRegisteredTypes(IIteratorHandlerServiceRegisterable registerable)
    {
        foreach (var action in RegistrationActions)
        {
            action(registerable);
        }
    }

    private static void RegisterHandlerTypeInternal<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >()
        where THandler : class, IIteratorHandler => RegistrationActions.Add(r => r.Register<THandler>());
}

internal interface IIteratorHandlerServiceRegisterable
{
    void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
        where THandler : class, IIteratorHandler;
}
