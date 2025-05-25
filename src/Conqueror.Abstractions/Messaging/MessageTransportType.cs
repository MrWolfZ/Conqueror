using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public readonly struct MessageTransportType(string name, MessageTransportRole role) : IEquatable<MessageTransportType>
{
    public string Name { get; } = name;

    public MessageTransportRole Role { get; } = role;

    public bool Equals(MessageTransportType other)
    {
        return Name == other.Name
               && Role == other.Role;
    }

    public override bool Equals(object? obj)
    {
        return obj is MessageTransportType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, (int)Role);
    }

    public static bool operator ==(MessageTransportType left, MessageTransportType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MessageTransportType left, MessageTransportType right)
    {
        return !(left == right);
    }
}

public enum MessageTransportRole
{
    Sender,
    Receiver,
}
