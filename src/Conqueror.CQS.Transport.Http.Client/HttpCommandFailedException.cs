using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;

// the standard constructors don't make sense
#pragma warning disable CA1032

namespace Conqueror.CQS.Transport.Http.Client
{
    [Serializable]
    public sealed class HttpCommandFailedException : Exception
    {
        public HttpCommandFailedException(string message, HttpResponseMessage response, Exception? innerException = null)
            : base(message, innerException)
        {
            Response = response;
        }

        private HttpCommandFailedException()
        {
        }

        private HttpCommandFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public HttpResponseMessage Response { get; } = default!;

        public HttpStatusCode StatusCode => Response.StatusCode;
    }
}
