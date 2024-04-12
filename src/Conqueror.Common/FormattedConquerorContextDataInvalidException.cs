using System;

namespace Conqueror.Common;

/// <summary>
///     An exception that represents badly formatted Conqueror context data.
/// </summary>
[Serializable]
public sealed class FormattedConquerorContextDataInvalidException : Exception
{
    public FormattedConquerorContextDataInvalidException(string message)
        : base(message)
    {
    }

    public FormattedConquerorContextDataInvalidException()
    {
    }

    public FormattedConquerorContextDataInvalidException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
