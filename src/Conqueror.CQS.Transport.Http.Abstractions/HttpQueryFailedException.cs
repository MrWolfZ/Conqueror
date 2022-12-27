using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;

// the standard constructors don't make sense
#pragma warning disable CA1032

namespace Conqueror
{
    [Serializable]
    public sealed class HttpQueryFailedException : Exception
    {
        public HttpQueryFailedException(string message, HttpResponseMessage response, Exception? innerException = null)
            : base(message, innerException)
        {
            Response = response;
        }

        private HttpQueryFailedException()
        {
        }

        private HttpQueryFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public HttpResponseMessage Response { get; } = default!;

        public HttpStatusCode StatusCode => Response.StatusCode;
    }
}
