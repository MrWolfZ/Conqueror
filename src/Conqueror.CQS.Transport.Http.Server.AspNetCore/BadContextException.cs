using System;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    [SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "this exception is used only internally and is ensured to not leak to the outside")]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "this exception is used only internally and does not require standard constructors")]
    internal sealed class BadContextException : Exception
    {
    }
}
