using System;
using System.Runtime.Serialization;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client
{
    [Serializable]
    public sealed class HttpInteractiveStreamingException : Exception
    {
        public HttpInteractiveStreamingException(string message)
            : base(message)
        {
        }

        public HttpInteractiveStreamingException(string message, Exception? innerException)
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
