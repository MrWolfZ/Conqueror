using System;

namespace Conqueror.Streaming.Transport.Http.Client;

[Serializable]
public sealed class HttpStreamingException : Exception
{
    public HttpStreamingException(string message)
        : base(message)
    {
    }

    public HttpStreamingException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    private HttpStreamingException()
    {
    }
}
