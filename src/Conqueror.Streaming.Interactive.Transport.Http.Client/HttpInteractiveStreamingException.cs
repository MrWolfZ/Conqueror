using System;
using System.Runtime.Serialization;

// the standard constructors don't make sense
#pragma warning disable CA1032

namespace Conqueror.Streaming.Interactive.Transport.Http.Client
{
    [Serializable]
    public sealed class HttpInteractiveStreamingException : Exception
    {
        public HttpInteractiveStreamingException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }

        private HttpInteractiveStreamingException()
        {
        }

        private HttpInteractiveStreamingException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}
