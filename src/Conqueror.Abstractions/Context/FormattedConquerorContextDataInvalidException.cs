using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     An exception that represents badly formatted Conqueror context data.
/// </summary>
public sealed class FormattedConquerorContextDataInvalidException : Exception
{
    public FormattedConquerorContextDataInvalidException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public FormattedConquerorContextDataInvalidException(string message)
        : base(message)
    {
    }

    public FormattedConquerorContextDataInvalidException()
    {
    }
}
