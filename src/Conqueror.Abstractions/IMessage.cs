// ReSharper disable ArrangeModifiersOrder
// ReSharper disable UnusedTypeParameter

#pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
#pragma warning disable S2326 // The type parameters are helpers and are used by type inference

namespace Conqueror;

public interface IMessage<TResponse>;

public interface IMessage : IMessage<UnitMessageResponse>;

public sealed record MessageTypes<TMessage>;

public record MessageTypes<TMessage, TResponse>;
