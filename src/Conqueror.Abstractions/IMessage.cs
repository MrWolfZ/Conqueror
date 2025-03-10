// ReSharper disable UnusedTypeParameter

namespace Conqueror;

public interface IMessage<TResponse>;

public interface IMessage : IMessage<UnitMessageResponse>;
