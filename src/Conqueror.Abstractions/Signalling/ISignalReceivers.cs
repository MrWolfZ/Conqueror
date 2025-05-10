using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     This interface has no own methods and only exists to allow transport packages to attach
///     extension methods for a uniform API (i.e. users only need to know about this interface
///     instead of having to inject a different interface for each transport package).
/// </summary>
public interface ISignalReceivers
{
    IServiceProvider ServiceProvider { get; }
}
