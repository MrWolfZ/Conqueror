using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public readonly struct SignalTransportType(string name, SignalTransportRole role) : IEquatable<SignalTransportType>
{
    public string Name { get; } = name;

    public SignalTransportRole Role { get; } = role;

    public bool Equals(SignalTransportType other)
    {
        return Name == other.Name
               && Role == other.Role;
    }

    public override bool Equals(object? obj)
    {
        return obj is SignalTransportType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, (int)Role);
    }

    public static bool operator ==(SignalTransportType left, SignalTransportType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SignalTransportType left, SignalTransportType right)
    {
        return !(left == right);
    }
}

public enum SignalTransportRole
{
    Publisher,
    Receiver,
}
