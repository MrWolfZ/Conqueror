using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror.Middleware.Logging;

[SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "this exception is never thrown, just logged, so no need to be public")]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "not needed here")]
internal sealed class WrappingException(Exception wrapped, string stackTrace) : Exception
{
    public override string Message => wrapped.Message;

    public override string StackTrace => wrapped.StackTrace + stackTrace;

    public override IDictionary Data => wrapped.Data;

    public override string? Source
    {
        get => wrapped.Source;
        set => wrapped.Source = value;
    }

    public override string? HelpLink
    {
        get => wrapped.HelpLink;
        set => wrapped.HelpLink = value;
    }

    public override string ToString() => wrapped + Environment.NewLine + stackTrace;

    public override Exception GetBaseException() => wrapped.GetBaseException();

    public override bool Equals(object? obj) => wrapped.Equals(obj);

    public override int GetHashCode() => wrapped.GetHashCode();
}
