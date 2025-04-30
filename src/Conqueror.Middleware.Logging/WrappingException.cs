using System;
using System.Collections;
using System.Collections.Generic;
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

    public override string ToString() => wrapped + Environment.NewLine + GetCleanStackTrace(stackTrace);

    public override Exception GetBaseException() => wrapped.GetBaseException();

    public override bool Equals(object? obj) => wrapped.Equals(obj);

    public override int GetHashCode() => wrapped.GetHashCode();

    private static string GetCleanStackTrace(string stackTrace)
    {
        return string.Join(Environment.NewLine, GetLines(stackTrace.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)));

        static IEnumerable<string> GetLines(string[] lines)
        {
            var skipNext = false;
            foreach (var line in lines)
            {
                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }

                if (line.TrimStart().StartsWith("at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine]"))
                {
                    skipNext = true;
                    continue;
                }

                yield return line;
            }
        }
    }
}
