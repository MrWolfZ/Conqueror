using System;

#pragma warning disable // this exception is used only internally and does not require all the typical aspects that other exceptions require

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class BadContextException : Exception
    {
    }
}
